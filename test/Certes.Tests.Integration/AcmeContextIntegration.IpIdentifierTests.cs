using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using Xunit;
using Xunit.Abstractions;

using static Certes.Helper;
using static Certes.IntegrationHelper;

namespace Certes
{
    public partial class AcmeContextIntegration
    {
        public class IpIdentifierTests : AcmeContextIntegration
        {
            // challtestsrv's fixed address in scripts/Pebble/compose.yml; it answers HTTP-01 on port 5002.
            private const string ChallengeServerAddress = "10.30.50.3";

            public IpIdentifierTests(ITestOutputHelper output) : base(output)
            {
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public async Task CanIssueShortlivedIpAddressCertificate(bool typedIdentifiers)
            {
                var ctx = NewAcmeContext(await GetAcmeUriV2(), GetKeyV2());
                var order = typedIdentifiers
                    ? await Authorize(ctx, new[] { new Identifier { Type = IdentifierType.Ip, Value = ChallengeServerAddress } },
                        ChallengeTypes.Http01, "shortlived")
                    : await Authorize(ctx, new[] { ChallengeServerAddress }, ChallengeTypes.Http01, "shortlived");
                var authz = Assert.Single(await order.Authorizations());
                var identifier = (await authz.Resource()).Identifier;
                Assert.Equal(IdentifierType.Ip, identifier.Type);
                Assert.Equal(ChallengeServerAddress, identifier.Value);

                var key = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var chain = await order.Generate(new CsrInfo(), key, retryCount: 120);
                var certificate = new X509CertificateParser().ReadCertificate(chain.Certificate.ToDer());
                var name = Assert.Single(GeneralNames.GetInstance(X509ExtensionUtilities.FromExtensionValue(
                    certificate.GetExtensionValue(X509Extensions.SubjectAlternativeName))).GetNames());
                Assert.Equal(GeneralName.IPAddress, name.TagNo);
                Assert.Equal(IPAddress.Parse(ChallengeServerAddress).GetAddressBytes(), Asn1OctetString.GetInstance(name.Name).GetOctets());
                Assert.InRange((certificate.NotAfter - certificate.NotBefore).TotalDays, 5.9, 6.1);
                Assert.Equal("shortlived", (await order.Resource()).Profile);

                await ClearAuthorizations(order);
            }
        }
    }
}

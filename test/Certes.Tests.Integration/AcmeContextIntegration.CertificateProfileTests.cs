using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Org.BouncyCastle.X509;
using Xunit;
using Xunit.Abstractions;

using static Certes.Helper;
using static Certes.IntegrationHelper;

namespace Certes
{
    public partial class AcmeContextIntegration
    {
        public class CertificateProfileTests : AcmeContextIntegration
        {
            public CertificateProfileTests(ITestOutputHelper output) : base(output)
            {
            }

            [Fact]
            public async Task CanIssueShortlivedProfileAndRequestReplacement()
            {
                var ctx = NewAcmeContext(await GetAcmeUriV2(), GetKeyV2());
                var directory = await ctx.GetDirectory();
                Assert.Contains("shortlived", directory.Meta.Profiles.Keys);
                var hosts = new[] { "profiles.example.test" };
                var order = await Authorize(ctx, hosts, ChallengeTypes.Http01, "shortlived");
                Assert.Equal("shortlived", (await order.Resource()).Profile);

                var key = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var chain = await order.Generate(new CsrInfo { CommonName = hosts[0] }, key, retryCount: 120);
                var certificate = new X509CertificateParser().ReadCertificate(chain.Certificate.ToDer());
                Assert.InRange((certificate.NotAfter - certificate.NotBefore).TotalDays, 5.9, 6.1);
                Assert.Equal("shortlived", (await order.Resource()).Profile);

                var certificateId = chain.GetRenewalInfoCertificateId();
                var replacement = await ctx.NewOrderWithProfile(hosts, "shortlived", replacedCertificateId: certificateId);
                var resource = await replacement.Resource();
                Assert.Equal("shortlived", resource.Profile);
                Assert.Equal(certificateId, resource.Replaces);
                await ClearAuthorizations(order);
            }
        }
    }
}

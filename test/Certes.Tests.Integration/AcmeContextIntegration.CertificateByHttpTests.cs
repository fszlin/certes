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
        public class CertificateByHttpTests : AcmeContextIntegration
        {
            public CertificateByHttpTests(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public async Task CanGenerateCertificateHttp()
            {
                var dirUri = await GetAcmeUriV2();
                var hosts = new[] { $"www-http-es256.certes-ci.dymetis.com", $"mail-http-es256.certes-ci.dymetis.com" };
                var ctx = new AcmeContext(dirUri, GetKeyV2(), http: GetAcmeHttpClient(dirUri));
                var orderCtx = await AuthorizeHttp(ctx, hosts);

                var certKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
                var finalizedOrder = await orderCtx.Finalize(new CsrInfo
                {
                    CountryName = "CA",
                    State = "Ontario",
                    Locality = "Toronto",
                    Organization = "Certes",
                    OrganizationUnit = "Dev",
                    CommonName = hosts[0],
                }, certKey);
                var certChain = await orderCtx.Download(null);

                var pfxBuilder = certChain.ToPfx(certKey);
                pfxBuilder.AddTestCerts();

                var pfx = pfxBuilder.Build("my-pfx", "abcd1234");

                // revoke certificate
                var certParser = new X509CertificateParser();
                var certificate = certParser.ReadCertificate(certChain.Certificate.ToDer());
                var der = certificate.GetEncoded();

                await ctx.RevokeCertificate(der, RevocationReason.Unspecified, null);

                // deactivate authz so the subsequence can trigger challenge validation
                await ClearAuthorizations(orderCtx);
            }
        }
    }
}

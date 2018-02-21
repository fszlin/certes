using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                var hosts = new[] { $"www-http-{DomainSuffix}.es256.certes-ci.dymetis.com", $"mail-http-{DomainSuffix}.es256.certes-ci.dymetis.com" };
                var ctx = new AcmeContext(dirUri, GetKeyV2(), http: GetAcmeHttpClient(dirUri));
                var orderCtx = await ctx.NewOrder(hosts);
                var order = await orderCtx.Resource();
                Assert.NotNull(order);
                Assert.Equal(hosts.Length, order.Authorizations?.Count);
                Assert.True(OrderStatus.Pending == order.Status || OrderStatus.Processing == order.Status);

                var authrizations = await orderCtx.Authorizations();

                foreach (var authz in authrizations)
                {
                    var httpChallenge = await authz.Http();
                    await httpChallenge.Validate();
                }

                while (true)
                {
                    await Task.Delay(100);

                    var statuses = new List<AuthorizationStatus>();
                    foreach (var authz in authrizations)
                    {
                        var a = await authz.Resource();
                        statuses.Add(a.Status ?? AuthorizationStatus.Pending);
                    }

                    if (statuses.All(s => s == AuthorizationStatus.Valid || s == AuthorizationStatus.Invalid))
                    {
                        break;
                    }
                }

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
                var certChain = await orderCtx.Download();

                var pfxBuilder = certChain.ToPfx(certKey);
                pfxBuilder.AddIssuers(IntegrationHelper.TestCertificates);

                var pfx = pfxBuilder.Build("my-pfx", "abcd1234");

                // revoke certificate
                var certParser = new X509CertificateParser();
                var certificate = certParser.ReadCertificate(Encoding.UTF8.GetBytes(certChain.Certificate));
                var der = certificate.GetEncoded();

                await ctx.RevokeCertificate(der, RevocationReason.Unspecified, null);

                // deactivate authz so the subsequence can trigger challenge validation
                foreach (var authz in authrizations)
                {
                    var authzRes = await authz.Deactivate();
                    Assert.Equal(AuthorizationStatus.Deactivated, authzRes.Status);
                }
            }
        }
    }
}

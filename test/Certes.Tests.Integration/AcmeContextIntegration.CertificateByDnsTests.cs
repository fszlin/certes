using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Pkcs;
using Xunit;
using Xunit.Abstractions;

using static Certes.Helper;
using static Certes.IntegrationHelper;

namespace Certes
{
    public partial class AcmeContextIntegration
    {
        public class CertificateByDnsTests : AcmeContextIntegration
        {
            public CertificateByDnsTests(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public async Task CanGenerateCertificateDns()
            {
                var dirUri = await GetAcmeUriV2();

                var hosts = new[] { $"www-dns-{DomainSuffix}.es256.certes-ci.dymetis.com", $"mail-dns-{DomainSuffix}.es256.certes-ci.dymetis.com" };
                var ctx = new AcmeContext(dirUri, GetKeyV2(), http: GetAcmeHttpClient(dirUri));
                var orderCtx = await AuthzDns(ctx, hosts);
                while (orderCtx == null)
                {
                    Output.WriteLine("DNS authz faild, retrying...");
                    orderCtx = await AuthzDns(ctx, hosts);
                }

                var csr = new CertificationRequestBuilder();
                csr.AddName($"C=CA, ST=Ontario, L=Toronto, O=Certes, OU=Dev, CN={hosts[0]}");
                foreach (var h in hosts)
                {
                    csr.SubjectAlternativeNames.Add(h);
                }

                var der = csr.Generate();

                var finalizedOrder = await orderCtx.Finalize(der);
                var certificate = await orderCtx.Download();

                // deactivate authz so the subsequence can trigger challenge validation
                var authrizations = await orderCtx.Authorizations();
                foreach (var authz in authrizations)
                {
                    var authzRes = await authz.Deactivate();
                    Assert.Equal(AuthorizationStatus.Deactivated, authzRes.Status);
                }
            }
        }

    }
}

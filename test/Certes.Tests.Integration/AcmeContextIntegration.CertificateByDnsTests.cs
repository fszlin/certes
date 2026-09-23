using System.Threading.Tasks;
using Certes.Pkcs;
using Certes.Acme.Resource;
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

                var hosts = new[] { "www-dns.example.test", "mail-dns.example.test" };
                var ctx = NewAcmeContext(dirUri, GetKeyV2());
                var orderCtx = await AuthzDns(ctx, hosts);

                var csr = new CertificationRequestBuilder();
                csr.AddName($"C=CA, ST=Ontario, L=Toronto, O=Certes, OU=Dev, CN={hosts[0]}");
                foreach (var h in hosts)
                {
                    csr.SubjectAlternativeNames.Add(h);
                }

                var der = csr.Generate();

                var finalizedOrder = await orderCtx.Finalize(der);
                await WaitForOrder(orderCtx, OrderStatus.Valid);
                var certificate = await orderCtx.Download(null);
                AssertExport(certificate, csr.Key);

                await ClearAuthorizations(orderCtx);
            }
        }

    }
}

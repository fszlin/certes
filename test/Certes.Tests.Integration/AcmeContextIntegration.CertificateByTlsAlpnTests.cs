using System.Text;
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
        public class CertificateByTlsAlpnTests : AcmeContextIntegration
        {
            public CertificateByTlsAlpnTests(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public async Task CanGenerateCertificateTlsAlpn()
            {
                var dirUri = await GetAcmeUriV2();
                var hosts = new[] { $"alpn-{DomainSuffix}.certes-ci.dymetis.com" };
                var ctx = new AcmeContext(dirUri, GetKeyV2(), http: GetAcmeHttpClient(dirUri));
                var orderCtx = await ctx.NewOrder(hosts);
                var order = await orderCtx.Resource();
                Assert.NotNull(order);
                Assert.Equal(hosts.Length, order.Authorizations?.Count);
                Assert.True(OrderStatus.Pending == order.Status || OrderStatus.Processing == order.Status);

                var authrizations = await orderCtx.Authorizations();

                foreach (var authz in authrizations)
                {
                    var tlsAlpnChallenge = await authz.TlsAlpn();
                    var certKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                    var alpnCert = ctx.AccountKey.TlsAlpnCertificate(tlsAlpnChallenge.Token, hosts[0], certKey);

                    var builder = new PfxBuilder(Encoding.UTF8.GetBytes(alpnCert), certKey);
                    var pfx = builder.Build(hosts[0], "abcd1234");
                }
            }
        }
    }
}

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Json;
using Newtonsoft.Json;
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
                var hosts = new[] { $"{DomainSuffix}.tls-alpn.certes-ci.dymetis.com" };
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

                    var certC = new CertificateChain(alpnCert);
                    var json = JsonConvert.SerializeObject(new
                    {
                        Cert = certC.Certificate.ToDer(),
                        Key = certKey.ToDer(),
                    }, JsonUtil.CreateSettings());

                    // setup validation certificate
                    using (var resp = await http.Value.PostAsync(
                            $"https://{hosts[0]}:443/tls-alpn-01/{hosts[0]}",
                            new StringContent(json, Encoding.UTF8, "application/json")))
                    {
                        Assert.Equal(hosts[0], await resp.Content.ReadAsStringAsync());
                        
                    }

                    await tlsAlpnChallenge.Validate();
                }

                // TODO: create certificate
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;
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
                var hosts = new[] { $"{Guid.NewGuid():N}.tls-alpn.example.test" };
                var ctx = new AcmeContext(dirUri, GetKeyV2(), http: GetAcmeHttpClient(dirUri));
                var order = await Authorize(ctx, hosts, ChallengeTypes.TlsAlpn01);
                var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
                await order.Finalize(new CsrInfo { CommonName = hosts[0] }, key);
                await WaitForOrder(order, OrderStatus.Valid);
                var chain = await order.Download();
                AssertExport(chain, key);
                await ctx.RevokeCertificate(chain.Certificate.ToDer(), RevocationReason.Unspecified, null);
                await ClearAuthorizations(order);
            }
        }
    }
}

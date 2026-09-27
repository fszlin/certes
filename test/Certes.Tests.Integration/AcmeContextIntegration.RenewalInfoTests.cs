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
        public class RenewalInfoTests : AcmeContextIntegration
        {
            public RenewalInfoTests(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public async Task CanGetRenewalInfoAndCreateReplacementOrder()
            {
                var dirUri = await GetAcmeUriV2();
                var hosts = new[] { "www-ari.example.test" };
                var ctx = NewAcmeContext(dirUri, GetKeyV2());
                var orderCtx = await AuthorizeHttp(ctx, hosts);

                var certKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var certChain = await orderCtx.Generate(new CsrInfo
                {
                    CommonName = hosts[0],
                }, certKey, preferredChain: null, retryCount: 120);

                var certificateId = certChain.GetRenewalInfoCertificateId();
                var renewalInfo = await ctx.GetRenewalInfo(certificateId);

                Assert.NotNull(renewalInfo.SuggestedWindow);
                Assert.True(renewalInfo.SuggestedWindow.Start < renewalInfo.SuggestedWindow.End);

                var replacement = await ctx.NewReplacementOrder(hosts, certificateId);
                var resource = await replacement.Resource();
                Assert.True(resource.Status == OrderStatus.Pending || resource.Status == OrderStatus.Ready,
                    $"Unexpected replacement order status: {resource.Status}");

                await ClearAuthorizations(orderCtx);
            }
        }
    }
}

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

using static Certes.Helper;
using static Certes.IntegrationHelper;

namespace Certes
{
    public partial class AcmeContextIntegration
    {
        public class AccountDiscoveryTests : AcmeContextIntegration
        {
            public AccountDiscoveryTests(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public async Task CanDiscoverAccountByKey()
            {
                var dirUri = await GetAcmeUriV2();

                var ctx = new AcmeContext(dirUri, GetKeyV2(), GetAcmeHttpClient(dirUri));
                var acct = await ctx.Account();

                Assert.NotNull(acct.Location);

                var res = await acct.Resource();
            }
        }
    }
}

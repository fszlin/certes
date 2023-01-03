using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Xunit;
using Xunit.Abstractions;
using static Certes.IntegrationHelper;

namespace Certes
{
    public partial class AcmeContextIntegration
    {
        public class AccountFlowTests : AcmeContextIntegration
        {
            public AccountFlowTests(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public async Task CanRunAccountFlows()
            {
                var dirUri = await GetAcmeUriV2();

                var ctx = new AcmeContext(dirUri, http: GetAcmeHttpClient(dirUri));
                var accountCtx = await ctx.NewAccount(
                    new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@certes.app" }, true);
                var account = await accountCtx.Resource();
                var location = accountCtx.Location;

                Assert.NotNull(account);
                Assert.Equal(AccountStatus.Valid, account.Status);

                await accountCtx.Update(contact: new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@certes.app" });

                account = await accountCtx.Deactivate();
                Assert.NotNull(account);
                Assert.Equal(AccountStatus.Deactivated, account.Status);
            }
        }
    }
}

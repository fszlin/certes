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

                var ctx = NewAcmeContext(dirUri);
                var accountCtx = await ctx.NewAccount(
                    new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@certes.app" }, true);
                var account = await accountCtx.Resource();
                var location = accountCtx.Location;

                Assert.NotNull(account);
                Assert.Equal(AccountStatus.Valid, account.Status);

                var newContact = $"mailto:certes-updated-{DateTime.UtcNow.Ticks}@certes.app";
                var updated = await accountCtx.Update(contact: new[] { newContact });
                Assert.Equal(new[] { newContact }, updated.Contact);

                // The same context reflects the update without being recreated (#286).
                Assert.Same(accountCtx, await ctx.Account());
                Assert.Equal(new[] { newContact }, (await (await ctx.Account()).Resource()).Contact);

                account = await accountCtx.Deactivate();
                Assert.NotNull(account);
                Assert.Equal(AccountStatus.Deactivated, account.Status);
            }
        }
    }
}

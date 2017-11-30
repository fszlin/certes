using System;
using System.Net.Http;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;
using Xunit;

namespace Certes
{
    public class IntegrationTests
    {
        private static Uri stagingServer;

        [Fact]
        public async Task CanRunAccountFlows()
        {
            var dirUri = await GetAvailableStagingServer();

            var ctx = new AcmeContext(dirUri);
            var account = await ctx.CreateAccount(new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@example.com" });
            var location = await ctx.GetAccountLocation();

            Assert.NotNull(account);
            Assert.Equal(AccountStatus.Valid, account.Status);

            await ctx.Account.Update(true);
            await ctx.Account.Update(contact: new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@example.com" });

            account = await ctx.Account.Resource();

            //var newKey = new AccountKey();
            //await ctx.ChangeKey(newKey);

            //var ctxWithNewKey = new AcmeContext(dirUri, newKey);
            //var locationWithNewKey = await ctxWithNewKey.GetAccountLocation();
            //Assert.Equal(location, locationWithNewKey);

            account = await ctx.Account.Deactivate();
            Assert.NotNull(account);
            Assert.Equal(AccountStatus.Deactivated, account.Status);
        }

        private async Task<Uri> GetAvailableStagingServer()
        {
            if (stagingServer != null)
            {
                return stagingServer;
            }

            var servers = new[] {
                new Uri("http://localhost:4001/directory"),
                new Uri("http://boulder-certes-ci.dymetis.com:4001/directory"),
            };

            using (var http = new HttpClient())
            {
                foreach (var uri in servers)
                {
                    try
                    {
                        await http.GetStringAsync(uri);
                        return stagingServer = uri;
                    }
                    catch
                    {
                    }
                }
            }

            throw new Exception("No staging server available.");
        }
    }
}

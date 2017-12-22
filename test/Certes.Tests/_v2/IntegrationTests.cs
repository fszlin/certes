using System;
using System.Net.Http;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Jws;
using Certes.Pkcs;
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
            var account = await ctx.NewAccount(
                new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@example.com" }, true);
            var location = await ctx.GetAccountLocation();

            Assert.NotNull(account);
            Assert.Equal(AccountStatus.Valid, account.Status);

            await ctx.Account.Update(true);
            await ctx.Account.Update(contact: new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@example.com" });

            account = await ctx.Account.Deactivate();
            Assert.NotNull(account);
            Assert.Equal(AccountStatus.Deactivated, account.Status);
        }

        [Fact(Skip = "New key is already in use for a different account")]
        public async Task CanChangeAccountKey()
        {
            var dirUri = await GetAvailableStagingServer();

            var ctx = new AcmeContext(dirUri);
            var account = await ctx.NewAccount(
                new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@example.com" }, true);
            var location = await ctx.GetAccountLocation();

            var newKey = new AccountKey();
            await ctx.ChangeKey(newKey);

            var ctxWithNewKey = new AcmeContext(dirUri, newKey);
            var locationWithNewKey = await ctxWithNewKey.GetAccountLocation();
            Assert.Equal(location, locationWithNewKey);
        }

        [Fact]
        public async Task CanCreateNewOrder()
        {
            var ctx = new AcmeContext(await GetAvailableStagingServer(), GetAccountKey());
            var orderCtx = await ctx.NewOrder(new[] { "www.certes-ci.certes.com", "mail.certes-ci.certes.com" });
            Assert.IsAssignableFrom<OrderContext>(orderCtx);
            var order = await orderCtx.Resource();
            Assert.NotNull(order);
            Assert.Equal(2, order.Authorizations?.Count);
            Assert.Equal(OrderStatus.Pending, order.Status);
        }

        private const string PrivateKey = "ME0CAQAwEwYHKoZIzj0CAQYIKoZIzj0DAQcEMzAxAgEBBCBePIf6wd4Gob+TzAdwp1/Pyz1tXQT22BoawjhdJRhAUaAKBggqhkjOPQMBBw==";

        private AccountKey GetAccountKey()
        {
            var keyInfo = new KeyInfo { PrivateKeyInfo = Convert.FromBase64String(PrivateKey) };
            return new AccountKey(keyInfo);
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

                        try
                        {
                            var ctx = new AcmeContext(uri, GetAccountKey());
                            await ctx.NewAccount(new[] { "mailto:fszlin@gmail.com" }, true);
                        }
                        catch
                        {
                        }

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

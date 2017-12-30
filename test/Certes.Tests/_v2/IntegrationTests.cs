using System;
using System.Collections.Generic;
using System.Linq;
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

            await ctx.Account.Update(agreeTermsOfService: true);
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
        public async Task CanGenerateCertificateDns()
        {
            var ctx = new AcmeContext(await GetAvailableStagingServer(), Helper.GetAccountKey());
            var orderCtx = await ctx.NewOrder(new[] { "www.es256.certes-ci.dymetis.com", "mail.es256.certes-ci.dymetis.com" });
            Assert.IsAssignableFrom<OrderContext>(orderCtx);
            var order = await orderCtx.Resource();
            Assert.NotNull(order);
            Assert.Equal(2, order.Authorizations?.Count);
            Assert.Equal(OrderStatus.Pending, order.Status);

            var authrizations = await orderCtx.Authorizations();

            foreach (var authz in authrizations)
            {
                var res = await authz.Resource();
                var dnsChallenge = await authz.Dns();
                await Helper.DeployDns01(res.Identifier.Value, dnsChallenge.Token);

                await dnsChallenge.Validate();
            }

            while (true)
            {
                await Task.Delay(100);

                var statuses = new List<AuthorizationStatus>();
                foreach (var authz in authrizations)
                {
                    var a = await authz.Resource();
                    statuses.Add(a.Status ?? AuthorizationStatus.Pending);
                }

                if (statuses.All(s => s == AuthorizationStatus.Valid || s == AuthorizationStatus.Invalid))
                {
                    break;
                }
            }

            var csr = new CertificationRequestBuilder();
            csr.AddName("CN=CA, ST=Ontario, L=Toronto, O=Certes, OU=Dev, CN=www.es256.certes-ci.dymetis.com");
            csr.SubjectAlternativeNames.Add("www.es256.certes-ci.dymetis.com");
            csr.SubjectAlternativeNames.Add("mail.es256.certes-ci.dymetis.com");
            var der = csr.Generate();

            var finalizedOrder = await orderCtx.Finalize(der);
            var certificate = await orderCtx.Download();

            // deactivate authz so the subsequence can trigger challenge validation
            foreach (var authz in authrizations)
            {
                var authzRes = await authz.Deactivate();
                Assert.Equal(AuthorizationStatus.Deactivated, authzRes.Status);
            }
        }

        [Fact]
        public async Task CanGenerateCertificateHttp()
        {
            var ctx = new AcmeContext(await GetAvailableStagingServer(), Helper.GetAccountKey());
            var orderCtx = await ctx.NewOrder(new[] { "www.es256.certes-ci.dymetis.com", "mail.es256.certes-ci.dymetis.com" });
            Assert.IsAssignableFrom<OrderContext>(orderCtx);
            var order = await orderCtx.Resource();
            Assert.NotNull(order);
            Assert.Equal(2, order.Authorizations?.Count);
            Assert.Equal(OrderStatus.Pending, order.Status);

            var authrizations = await orderCtx.Authorizations();

            foreach (var authz in authrizations)
            {
                var httpChallenge = await authz.Http();
                await httpChallenge.Validate();
            }

            while (true)
            {
                await Task.Delay(100);

                var statuses = new List<AuthorizationStatus>();
                foreach (var authz in authrizations)
                {
                    var a = await authz.Resource();
                    statuses.Add(a.Status ?? AuthorizationStatus.Pending);
                }

                if (statuses.All(s => s == AuthorizationStatus.Valid || s == AuthorizationStatus.Invalid))
                {
                    break;
                }
            }

            var csr = new CertificationRequestBuilder();
            csr.AddName("CN=CA, ST=Ontario, L=Toronto, O=Certes, OU=Dev, CN=www.es256.certes-ci.dymetis.com");
            csr.SubjectAlternativeNames.Add("www.es256.certes-ci.dymetis.com");
            csr.SubjectAlternativeNames.Add("mail.es256.certes-ci.dymetis.com");
            var der = csr.Generate();

            var finalizedOrder = await orderCtx.Finalize(der);
            var certificate = await orderCtx.Download();

            // deactivate authz so the subsequence can trigger challenge validation
            foreach (var authz in authrizations)
            {
                var authzRes = await authz.Deactivate();
                Assert.Equal(AuthorizationStatus.Deactivated, authzRes.Status);
            }
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
                            var ctx = new AcmeContext(uri, Helper.GetAccountKey());
                            await ctx.NewAccount(new[] { "mailto:fszlin@example.com" }, true);
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

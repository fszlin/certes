using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Jws;
using Certes.Pkcs;
using Org.BouncyCastle.X509;
using Xunit;
using Xunit.Abstractions;

namespace Certes
{
    public class IntegrationTests
    {
        private static Uri stagingServer;
        private readonly ITestOutputHelper output;
        private readonly string domainSuffix;

        public IntegrationTests(ITestOutputHelper output)
        {
            this.output = output;

            domainSuffix =
                bool.TrueString.Equals(Environment.GetEnvironmentVariable("APPVEYOR"), StringComparison.OrdinalIgnoreCase) ? "appveyor" :
                bool.TrueString.Equals(Environment.GetEnvironmentVariable("TRAVIS"), StringComparison.OrdinalIgnoreCase) ? "travis" :
                "dev";
        }

        [Fact]
        public async Task CanRunAccountFlows()
        {
            var dirUri = await GetAvailableStagingServer();

            var ctx = new AcmeContext(dirUri);
            var account = await ctx.NewAccount(
                new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@example.com" }, true);
            var location = await ctx.Account().Location();

            Assert.NotNull(account);
            Assert.Equal(AccountStatus.Valid, account.Status);

            await ctx.Account().Update(agreeTermsOfService: true);
            await ctx.Account().Update(contact: new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@example.com" });

            account = await ctx.Account().Deactivate();
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
            var location = await ctx.Account().Location();

            var newKey = new AccountKey();
            await ctx.ChangeKey(newKey);

            var ctxWithNewKey = new AcmeContext(dirUri, newKey);
            var locationWithNewKey = await ctxWithNewKey.Account().Location();
            Assert.Equal(location, locationWithNewKey);
        }

        [Fact]
        public async Task CanGenerateCertificateDns()
        {
            var hosts = new[] { $"www-dns-{domainSuffix}.es256.certes-ci.dymetis.com", $"mail-dns-{domainSuffix}.es256.certes-ci.dymetis.com" };
            var ctx = new AcmeContext(await GetAvailableStagingServer(), Helper.GetAccountKey());
            var orderCtx = await AuthzDns(ctx, hosts);
            while (orderCtx == null)
            {
                output.WriteLine("DNS authz faild, retrying...");
                await Task.Delay(1000);
                orderCtx = await AuthzDns(ctx, hosts);
            }

            var csr = new CertificationRequestBuilder();
            csr.AddName($"CN=CA, ST=Ontario, L=Toronto, O=Certes, OU=Dev, CN={hosts[0]}");
            foreach (var h in hosts)
            {
                csr.SubjectAlternativeNames.Add(h);
            }

            var der = csr.Generate();

            var finalizedOrder = await orderCtx.Finalize(der);
            var certificate = await orderCtx.Download();

            // deactivate authz so the subsequence can trigger challenge validation
            var authrizations = await orderCtx.Authorizations();
            foreach (var authz in authrizations)
            {
                var authzRes = await authz.Deactivate();
                Assert.Equal(AuthorizationStatus.Deactivated, authzRes.Status);
            }
        }

        [Fact]
        public async Task CanGenerateCertificateHttp()
        {
            var hosts = new[] { $"www-http-{domainSuffix}.es256.certes-ci.dymetis.com", $"mail-http-{domainSuffix}.es256.certes-ci.dymetis.com" };
            var ctx = new AcmeContext(await GetAvailableStagingServer(), Helper.GetAccountKey());
            var orderCtx = await ctx.NewOrder(hosts);
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
            csr.AddName($"CN=CA, ST=Ontario, L=Toronto, O=Certes, OU=Dev, CN={hosts[0]}");
            foreach (var h in hosts)
            {
                csr.SubjectAlternativeNames.Add(h);
            }

            var csrDer = csr.Generate();

            var finalizedOrder = await orderCtx.Finalize(csrDer);
            var pem = await orderCtx.Download();

            // revoke certificate
            var certParser = new X509CertificateParser();
            var certificate = certParser.ReadCertificate(Encoding.UTF8.GetBytes(pem));
            var der = certificate.GetEncoded();

            await ctx.RevokeCertificate(der, RevocationReason.Unspecified, null);

            // deactivate authz so the subsequence can trigger challenge validation
            foreach (var authz in authrizations)
            {
                var authzRes = await authz.Deactivate();
                Assert.Equal(AuthorizationStatus.Deactivated, authzRes.Status);
            }
        }

        private async Task<IOrderContext> AuthzDns(AcmeContext ctx, string[] hosts)
        {
            var orderCtx = await ctx.NewOrder(hosts);
            Assert.IsAssignableFrom<OrderContext>(orderCtx);
            var order = await orderCtx.Resource();
            Assert.NotNull(order);
            Assert.Equal(2, order.Authorizations?.Count);
            Assert.Equal(OrderStatus.Pending, order.Status);

            var authrizations = await orderCtx.Authorizations();

            var tokens = new Dictionary<string, string>();
            foreach (var authz in authrizations)
            {
                var res = await authz.Resource();
                var dnsChallenge = await authz.Dns();
                tokens.Add(res.Identifier.Value, dnsChallenge.Token);
            }

            await Helper.DeployDns01(SignatureAlgorithm.ES256, tokens);

            foreach (var authz in authrizations)
            {
                var res = await authz.Resource();
                var dnsChallenge = await authz.Dns();
                await dnsChallenge.Validate();
            }

            while (true)
            {
                await Task.Delay(100);

                var statuses = new List<AuthorizationStatus>();
                foreach (var authz in authrizations)
                {
                    var a = await authz.Resource();
                    if (AuthorizationStatus.Invalid == a.Status)
                    {
                        return null;
                    }
                    else
                    {
                        statuses.Add(a.Status ?? AuthorizationStatus.Pending);
                    }
                }

                if (statuses.All(s => s == AuthorizationStatus.Valid))
                {
                    break;
                }
            }

            return orderCtx;
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

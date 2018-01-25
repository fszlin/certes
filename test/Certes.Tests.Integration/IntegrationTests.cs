using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Pkcs;
using Org.BouncyCastle.X509;
using Xunit;
using Xunit.Abstractions;

namespace Certes
{
    [Collection(nameof(IntegrationTests))]
    public class IntegrationTests
    {
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
        public async Task CanDiscoverAccountByKey()
        {
            var dirUri = await IntegrationHelper.GetAcmeUriV2();

            var ctx = new AcmeContext(dirUri, Helper.GetKeyV2());
            var acct = await ctx.Account();

            Assert.NotNull(acct.Location);

            var res = await acct.Resource();
        }

        [Fact]
        public async Task CanRunAccountFlows()
        {
            var dirUri = await IntegrationHelper.GetAcmeUriV2();

            var ctx = new AcmeContext(dirUri);
            var accountCtx = await ctx.NewAccount(
                new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@example.com" }, true);
            var account = await accountCtx.Resource();
            var location = accountCtx.Location;

            Assert.NotNull(account);
            Assert.Equal(AccountStatus.Valid, account.Status);

            await accountCtx.Update(agreeTermsOfService: true);
            await accountCtx.Update(contact: new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@example.com" });

            account = await accountCtx.Deactivate();
            Assert.NotNull(account);
            Assert.Equal(AccountStatus.Deactivated, account.Status);
        }

        [Fact]
        public async Task CanChangeAccountKey()
        {
            var dirUri = await IntegrationHelper.GetAcmeUriV2();

            var ctx = new AcmeContext(dirUri);
            var account = await ctx.NewAccount(
                new[] { $"mailto:certes-{DateTime.UtcNow.Ticks}@example.com" }, true);
            var location = await ctx.Account().Location();

            var newKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            await ctx.ChangeKey(newKey);

            var ctxWithNewKey = new AcmeContext(dirUri, newKey);
            var locationWithNewKey = await ctxWithNewKey.Account().Location();
            Assert.Equal(location, locationWithNewKey);
        }

        [Fact(Skip = "https://github.com/letsencrypt/boulder/issues/3333")]
        public async Task CanGenerateCertificateDns()
        {
            var hosts = new[] { $"www-dns-{domainSuffix}.es256.certes-ci.dymetis.com", $"mail-dns-{domainSuffix}.es256.certes-ci.dymetis.com" };
            var ctx = new AcmeContext(await IntegrationHelper.GetAcmeUriV2(), Helper.GetKeyV2());
            var orderCtx = await AuthzDns(ctx, hosts);
            while (orderCtx == null)
            {
                output.WriteLine("DNS authz faild, retrying...");
                await Task.Delay(1000);
                orderCtx = await AuthzDns(ctx, hosts);
            }

            var csr = new CertificationRequestBuilder();
            csr.AddName($"C=CA, ST=Ontario, L=Toronto, O=Certes, OU=Dev, CN={hosts[0]}");
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

        [Fact(Skip = "https://github.com/letsencrypt/boulder/issues/3333")]
        public async Task CanGenerateWildcard()
        {
            var hosts = new[] { $"wildcard-{domainSuffix}.es256.certes-ci.dymetis.com" };
            var ctx = new AcmeContext(await IntegrationHelper.GetAcmeUriV2(), Helper.GetKeyV2());

            var orderCtx = await AuthzDns(ctx, hosts);
            var certKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var finalizedOrder = await orderCtx.Finalize(new CsrInfo
            {
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
                CommonName = hosts[0],
            }, certKey);
            var pem = await orderCtx.Download();
        }

        [Theory]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        public async Task CanGenerateCertificateWithEC(KeyAlgorithm algo)
        {
            var hosts = new[] { $"www-ec-{domainSuffix}.es256.certes-ci.dymetis.com".ToLower() };
            var ctx = new AcmeContext(await IntegrationHelper.GetAcmeUriV2(), Helper.GetKeyV2());
            var orderCtx = await ctx.NewOrder(hosts);
            var order = await orderCtx.Resource();
            Assert.NotNull(order);
            Assert.Equal(hosts.Length, order.Authorizations?.Count);
            Assert.True(OrderStatus.Pending == order.Status || OrderStatus.Processing == order.Status);

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

            var certKey = KeyFactory.NewKey(algo);
            var finalizedOrder = await orderCtx.Finalize(new CsrInfo
            {
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
                CommonName = hosts[0],
            }, certKey);
            var pem = await orderCtx.Download();

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
            var hosts = new[] { $"www-http-{domainSuffix}.es256.certes-ci.dymetis.com", $"mail-http-{domainSuffix}.es256.certes-ci.dymetis.com" };
            var ctx = new AcmeContext(await IntegrationHelper.GetAcmeUriV2(), Helper.GetKeyV2());
            var orderCtx = await ctx.NewOrder(hosts);
            var order = await orderCtx.Resource();
            Assert.NotNull(order);
            Assert.Equal(hosts.Length, order.Authorizations?.Count);
            Assert.True(OrderStatus.Pending == order.Status || OrderStatus.Processing == order.Status);

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

            var certKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var finalizedOrder = await orderCtx.Finalize(new CsrInfo
            { 
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
                CommonName = hosts[0],
            }, certKey);
            var pem = await orderCtx.Download();
            
            var pfxBuilder = new PfxBuilder(Encoding.UTF8.GetBytes(pem), certKey);
            pfxBuilder.AddIssuers(Helper.TestCertificates);

            var pfx = pfxBuilder.Build("my-pfx", "abcd1234");

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
            var order = await orderCtx.Resource();
            Assert.NotNull(order);
            Assert.Equal(2, order.Authorizations?.Count);
            Assert.True(OrderStatus.Pending == order.Status || OrderStatus.Processing == order.Status);

            var authrizations = await orderCtx.Authorizations();

            var tokens = new Dictionary<string, string>();
            foreach (var authz in authrizations)
            {
                var res = await authz.Resource();
                var dnsChallenge = await authz.Dns();
                tokens.Add(res.Identifier.Value, dnsChallenge.Token);
            }

            await IntegrationHelper.DeployDns01(KeyAlgorithm.ES256, tokens);

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

    }
}

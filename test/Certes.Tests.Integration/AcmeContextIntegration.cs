using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Pkcs;
using Org.BouncyCastle.X509;
using Xunit;
using Xunit.Abstractions;

using static Certes.Helper;
using static Certes.IntegrationHelper;

namespace Certes
{
    public partial class AcmeContextIntegration
    {
        protected ITestOutputHelper Output { get; private set; }
        protected string DomainSuffix { get; private set; }

        public AcmeContextIntegration(ITestOutputHelper output)
        {
            Output = output;
            DomainSuffix =
                bool.TrueString.Equals(Environment.GetEnvironmentVariable("APPVEYOR"), StringComparison.OrdinalIgnoreCase) ? "appveyor" :
                bool.TrueString.Equals(Environment.GetEnvironmentVariable("TRAVIS"), StringComparison.OrdinalIgnoreCase) ? "travis" :
                "dev";
        }

        //[Theory]
        //[InlineData(KeyAlgorithm.ES256)]
        //[InlineData(KeyAlgorithm.ES384)]
        protected async Task CanGenerateCertificateWithEC(KeyAlgorithm algo)
        {
            var dirUri = await GetAcmeUriV2();
            var hosts = new[] { $"www-ec-{DomainSuffix}.{algo}.certes-ci.dymetis.com".ToLower() };
            var ctx = new AcmeContext(dirUri, GetKeyV2(algo), http: GetAcmeHttpClient(dirUri));
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
            var cert = await orderCtx.Download();

            var certInfo = new CertificateInfo(cert, certKey);
            var x509 = new X509Certificate2(certInfo.ToDer());
            Assert.Contains(hosts[0], x509.Subject);

            // deactivate authz so the subsequence can trigger challenge validation
            foreach (var authz in authrizations)
            {
                var authzRes = await authz.Deactivate();
                Assert.Equal(AuthorizationStatus.Deactivated, authzRes.Status);
            }

        }

        protected async Task<IOrderContext> AuthzDns(AcmeContext ctx, string[] hosts)
        {
            var orderCtx = await ctx.NewOrder(hosts);
            var order = await orderCtx.Resource();
            Assert.NotNull(order);
            Assert.Equal(hosts.Length, order.Authorizations?.Count);
            Assert.True(OrderStatus.Pending == order.Status || OrderStatus.Processing == order.Status);

            var authrizations = await orderCtx.Authorizations();

            var tokens = new Dictionary<string, string>();
            foreach (var authz in authrizations)
            {
                var res = await authz.Resource();
                var dnsChallenge = await authz.Dns();
                tokens.Add(res.Identifier.Value, dnsChallenge.Token);
            }

            await DeployDns01(KeyAlgorithm.ES256, tokens);
            await Task.Delay(1000);

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

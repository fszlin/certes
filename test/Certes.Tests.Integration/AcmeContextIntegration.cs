using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Xunit;
using Xunit.Abstractions;

using static Certes.Helper;
using static Certes.IntegrationHelper;

namespace Certes
{
    public partial class AcmeContextIntegration
    {
        protected ITestOutputHelper Output { get; private set; }

        public AcmeContextIntegration(ITestOutputHelper output)
        {
            Output = output;
        }

        protected async Task CanGenerateCertificateWithEC(KeyAlgorithm algo)
        {
            var dirUri = await GetAcmeUriV2();
            var hosts = new[] { $"www-ec-{algo}.certes-ci.dymetis.com".ToLower() };
            var ctx = new AcmeContext(dirUri, GetKeyV2(algo), http: GetAcmeHttpClient(dirUri));
            var orderCtx = await AuthorizeHttp(ctx, hosts);

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
            var cert = await orderCtx.Download(null);

            var x509 = new X509Certificate2(cert.Certificate.ToDer());
            Assert.Contains(hosts[0], x509.Subject);

            // deactivate authz so the subsequence can trigger challenge validation
            await ClearAuthorizations(orderCtx);
        }

        protected async Task<IOrderContext> AuthzDns(AcmeContext ctx, string[] hosts)
        {
            var orderCtx = await ctx.NewOrder(hosts);
            var order = await orderCtx.Resource();
            Assert.NotNull(order);
            Assert.Equal(hosts.Length, order.Authorizations?.Count);
            Assert.True(
                OrderStatus.Pending == order.Status || OrderStatus.Processing == order.Status || OrderStatus.Ready == order.Status,
                $"actual: {order.Status}");

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
                if (res.Status == AuthorizationStatus.Pending)
                {
                    var dnsChallenge = await authz.Dns();
                    await dnsChallenge.Validate();
                }
            }

            while (true)
            {
                await Task.Delay(100);

                var statuses = new List<AuthorizationStatus>();
                foreach (var authz in authrizations)
                {
                    var a = await authz.Resource();
                    if (AuthorizationStatus.Invalid == a?.Status)
                    {
                        return null;
                    }
                    else
                    {
                        statuses.Add(a?.Status ?? AuthorizationStatus.Pending);
                    }
                }

                if (statuses.All(s => s == AuthorizationStatus.Valid))
                {
                    break;
                }
            }

            return orderCtx;
        }

        private static Task<IOrderContext> AuthorizeHttp(AcmeContext ctx, IList<string> hosts)
            => IntegrationHelper.AuthorizeHttp(ctx, hosts);

        private static async Task ClearAuthorizations(Acme.IOrderContext orderCtx)
        {
            // deactivate authz so the subsequence can trigger challenge validation
            var authrizations = await orderCtx.Authorizations();
            foreach (var authz in authrizations)
            {
                var authzRes = await authz.Deactivate();
                Assert.Equal(AuthorizationStatus.Deactivated, authzRes.Status);
            }
        }
    }
}

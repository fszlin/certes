using System.Collections.Generic;
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
            var hosts = new[] { $"www-ec-{algo}.example.test".ToLowerInvariant() };
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
            await WaitForOrder(orderCtx, OrderStatus.Valid);
            var cert = await orderCtx.Download(null);
            AssertExport(cert, certKey);

#if NET9_0_OR_GREATER
            using var x509 = X509CertificateLoader.LoadCertificate(cert.Certificate.ToDer());
#else
            using var x509 = new X509Certificate2(cert.Certificate.ToDer());
#endif
            Assert.Equal(hosts[0], x509.GetNameInfo(X509NameType.DnsName, false));

            // deactivate authz so the subsequence can trigger challenge validation
            await ClearAuthorizations(orderCtx);
        }

        protected Task<IOrderContext> AuthzDns(AcmeContext ctx, string[] hosts)
            => Authorize(ctx, hosts, ChallengeTypes.Dns01);

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

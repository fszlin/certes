using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Xunit;

namespace Certes
{
    public class Readme
    {
        [Fact]
        public async Task Account()
        {
            var acmeDir = await IntegrationHelper.GetAcmeUriV2();
            var accountKey = Helper.GetKeyV2(KeyAlgorithm.RS256);
            var acme = IntegrationHelper.NewAcmeContext(acmeDir, accountKey);
            var account = await acme.Account();

            var order = await acme.NewOrder(new[] { "readme.example.test" });

            var authz = (await order.Authorizations()).First();
            var httpChallenge = await authz.Http();

            var token = httpChallenge.Token;
            var keyAuthz = httpChallenge.KeyAuthz;

            var orderUri = order.Location;

            var res = await authz.Resource();
            if (res.Status == AuthorizationStatus.Pending)
            {
                await IntegrationHelper.ConfigureChallenge("add-http01", new { token, content = keyAuthz });
                try
                {
                    await httpChallenge.Validate();
                    await IntegrationHelper.WaitForAuthorization(authz);
                }
                finally
                {
                    await IntegrationHelper.ConfigureChallenge("del-http01", new { token });
                }
            }
            await IntegrationHelper.WaitForOrder(order, OrderStatus.Ready);

            acme = IntegrationHelper.NewAcmeContext(acmeDir, accountKey);
            order = acme.Order(orderUri);
            var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            // Keep the number of polling retries bounded independently of elapsed delay.
            var cert = await order.Generate(new CsrInfo
            {
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
                CommonName = "readme.example.test",
            }, privateKey, null, retryCount: 60);
            IntegrationHelper.AssertExport(cert, privateKey);

            var pfxBuilder = cert.ToPfx(privateKey);
            pfxBuilder.AddTestCerts();
            var pfx = pfxBuilder.Build("my-cert", "abcd1234");
        }
    }
}

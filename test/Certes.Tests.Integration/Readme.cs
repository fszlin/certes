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
            var httpClient = IntegrationHelper.GetAcmeHttpClient(acmeDir);

            var acme = new AcmeContext(acmeDir, accountKey, httpClient);
            var account = await acme.Account();

            var order = await acme.NewOrder(new[] { "www.certes-ci.dymetis.com" });

            var authz = (await order.Authorizations()).First();
            var httpChallenge = await authz.Http();

            var token = httpChallenge.Token;
            var keyAuthz = httpChallenge.KeyAuthz;

            var orderUri = order.Location;

            var res = await authz.Resource();
            if (res.Status == AuthorizationStatus.Pending)
            {
                await httpChallenge.Validate();
            }

            while (res?.Status != AuthorizationStatus.Valid && res?.Status != AuthorizationStatus.Invalid)
            {
                res = await authz.Resource();
            }

            acme = new AcmeContext(acmeDir, accountKey, httpClient);
            order = acme.Order(orderUri);
            var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var cert = await order.Generate(new CsrInfo
            {
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
                CommonName = "www.certes-ci.dymetis.com",
            }, privateKey, null);

            var pfxBuilder = cert.ToPfx(privateKey);
            pfxBuilder.AddTestCerts();
            var pfx = pfxBuilder.Build("my-cert", "abcd1234");
        }
    }
}

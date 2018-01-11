using System.Linq;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Xunit;

namespace Certes
{
    public class Readme
    {
        [Fact]
        public async Task Account()
        {
            var acmeDir = await Helper.GetAvailableStagingServerV2();
            var accountKey = Helper.GetKeyV2(KeyAlgorithm.RS256);

            var acme = new AcmeContext(acmeDir, accountKey);
            var account = await acme.NewAccount("admin@example.com", true);

            var order = await acme.NewOrder(new[] { "www.certes-ci.dymetis.com" });

            var authz = (await order.Authorizations()).First();
            var httpChallenge = await authz.Http();

            var token = httpChallenge.Token;
            var keyAuthz = httpChallenge.KeyAuthz;

            var orderUri = order.Location;

            await httpChallenge.Validate();

            var res = await authz.Resource();
            while (res.Status != AuthorizationStatus.Valid && res.Status != AuthorizationStatus.Invalid)
            {
                res = await authz.Resource();
            }

            acme = new AcmeContext(acmeDir, accountKey);
            order = acme.Order(orderUri);
            var cert = await order.Generate(new CsrInfo
            {
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
                CommonName = "www.certes-ci.dymetis.com",
            });

            cert.ToPfx("my-cert.pfx", "abcd1234", issuers: Helper.TestCertificates);
        }
    }
}

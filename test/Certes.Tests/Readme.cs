using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Pkcs;
using Xunit;

namespace Certes
{
    public class Readme
    {
        [Fact(Skip = "https://github.com/letsencrypt/boulder/issues/3335")]
        public async Task Account()
        {
            var acme = new AcmeContext(await IntegrationTests.GetAvailableStagingServer(), Helper.GetAccountKey(SignatureAlgorithm.RS256));
            var account = await acme.Account();

            var order = await acme.NewOrder(new[] { "www.certes-ci.dymetis.com" });

            var authz = (await order.Authorizations()).First();
            var httpChallenge = await authz.Http();
            var token = httpChallenge.Token;
            var keyAuthz = httpChallenge.KeyAuthz;

            // TODO: search the order/authz - https://github.com/letsencrypt/boulder/issues/3335
            await httpChallenge.Validate();

            var certKey = DSA.NewKey(SignatureAlgorithm.RS256);
            await order.Finalize(new CsrInfo
            {
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
                CommonName = "www.certes-ci.dymetis.com",
            }, certKey);

            var pem = await order.Download();

            await account.Deactivate();
        }
    }
}

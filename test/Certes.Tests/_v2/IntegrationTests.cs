using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Xunit;

namespace Certes
{
    public class IntegrationTests
    {
        private static readonly Uri interationDirectoryUri = new Uri("http://boulder-certes-ci.dymetis.com:4001/directory");

        [Fact]
        public async Task CanCreateNewAccount()
        {
            var ctx = new AcmeContext(interationDirectoryUri);
            var account = await ctx.CreateAccount(new[] { "mailto:certes@example.com" });

            Assert.NotNull(account);
            Assert.Equal(AccountStatus.Valid, account.Status);
            Assert.False(account.TermsOfServiceAgreed);

            //var location = await ctx.Account.GetLocation();
        }
    }
}

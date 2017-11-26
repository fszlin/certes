using System.Threading.Tasks;
using Xunit;

namespace Certes.Acme
{
    public class ChallengeTests
    {
        [Fact]
        public async Task CanComputeDnsKeyAuth()
        {
            var challenge = new Challenge
            {
                Token = "6csJt_REONi1guIpCqdw6wCP5hL8YxtOhTCETu7ECYY",
                Type = "dns-01"
            };

            var keyAuth = challenge.ComputeDnsValue(await Helper.Loadkey());
            Assert.Equal(
                "_R4B3fDaVztZshDzof1sXQ90V-JlADF_2WFua87u7qU",
                keyAuth);
        }
    }
}

using Xunit;

namespace Certes.Acme
{
    public class ChallengeTests
    {
        [Fact]
        public void CanComputeDnsKeyAuth()
        {
            var challenge = new Challenge
            {
                Token = "6csJt_REONi1guIpCqdw6wCP5hL8YxtOhTCETu7ECYY",
                Type = "dns-01"
            };

            var keyAuth = challenge.ComputeKeyAuthorization(Helper.Loadkey());
            Assert.Equal(
                "6csJt_REONi1guIpCqdw6wCP5hL8YxtOhTCETu7ECYY.cSTPyZ48ZK6w7Z_ndytGxoz5XNR6-ycwGEs_VI6nj44",
                keyAuth);
        }
    }
}

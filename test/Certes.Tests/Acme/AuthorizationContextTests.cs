using System;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Authz = Certes.Acme.Resource.Authorization;
using AuthzChallenge = Certes.Acme.Resource.Challenge;

namespace Certes.Acme
{
    public class AuthorizationContextTests
    {
        private Uri location = new Uri("http://acme.d/authz/101");
        private Mock<IAcmeContext> contextMock = new Mock<IAcmeContext>();
        private Mock<IAcmeHttpClient> httpClientMock = new Mock<IAcmeHttpClient>();

        [Fact]
        public async Task CanLoadChallenges()
        {
            var authz = new Authz
            {
                Challenges = new[] {
                    new AuthzChallenge
                    {
                        Url = new Uri("http://acme.d/c/1"),
                        Token = "token",
                        Type = "dns-01"
                    },
                    new AuthzChallenge
                    {
                        Url = new Uri("http://acme.d/c/1"),
                        Token = "token",
                        Type = "http-01"
                    }
                }
            };

            contextMock.Reset();
            httpClientMock.Reset();

            contextMock
                .Setup(c => c.GetDirectory())
                .ReturnsAsync(Helper.MockDirectoryV2);
            contextMock
                .SetupGet(c => c.AccountKey)
                .Returns(Helper.GetKeyV2());
            contextMock.SetupGet(c => c.HttpClient).Returns(httpClientMock.Object);
            httpClientMock
                .Setup(m => m.Get<Authz>(location))
                .ReturnsAsync(new AcmeHttpResponse<Authz>(location, authz, default, default));

            var ctx = new AuthorizationContext(contextMock.Object, location);
            var challenges = await ctx.Challenges();
            Assert.Equal(authz.Challenges.Select(c => c.Url), challenges.Select(a => a.Location));

            // check the context returns empty list instead of null
            httpClientMock
                .Setup(m => m.Get<Authz>(location))
                .ReturnsAsync(new AcmeHttpResponse<Authz>(location, new Authz(), default, default));
            challenges = await ctx.Challenges();
            Assert.Empty(challenges);

        }
    }
}

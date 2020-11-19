using System;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;
using Moq;
using Xunit;

namespace Certes.Acme
{
    public class AuthorizationContextTests
    {
        private Uri location = new Uri("http://acme.d/authz/101");
        private Mock<IAcmeContext> contextMock = new Mock<IAcmeContext>(MockBehavior.Strict);
        private Mock<IAcmeHttpClient> httpClientMock = new Mock<IAcmeHttpClient>(MockBehavior.Strict);

        [Fact]
        public async Task CanLoadChallenges()
        {
            var authz = new Authorization
            {
                Challenges = new[] {
                    new Challenge
                    {
                        Url = new Uri("http://acme.d/c/1"),
                        Token = "token",
                        Type = "dns-01"
                    },
                    new Challenge
                    {
                        Url = new Uri("http://acme.d/c/1"),
                        Token = "token",
                        Type = "http-01"
                    }
                }
            };

            var expectedPayload = new JwsSigner(Helper.GetKeyV2())
                .Sign("", null, location, "nonce");

            contextMock.Reset();
            httpClientMock.Reset();

            contextMock
                .Setup(c => c.GetDirectory())
                .ReturnsAsync(Helper.MockDirectoryV2);
            contextMock
                .SetupGet(c => c.AccountKey)
                .Returns(Helper.GetKeyV2());
            contextMock
                .SetupGet(c => c.BadNonceRetryCount)
                .Returns(1);
            contextMock
                .Setup(c => c.Sign(It.IsAny<object>(), location))
                .Callback((object payload, Uri loc) =>
                {
                    Assert.Null(payload);
                    Assert.Equal(location, loc);
                })
                .ReturnsAsync(expectedPayload);
            contextMock.SetupGet(c => c.HttpClient).Returns(httpClientMock.Object);
            httpClientMock
                .Setup(m => m.Post<Authorization>(location, It.IsAny<JwsPayload>()))
                .Callback((Uri _, object o) =>
                {
                    var p = (JwsPayload)o;
                    Assert.Equal(expectedPayload.Payload, p.Payload);
                    Assert.Equal(expectedPayload.Protected, p.Protected);
                })
                .ReturnsAsync(new AcmeHttpResponse<Authorization>(location, authz, default, default));

            var ctx = new AuthorizationContext(contextMock.Object, location);
            var challenges = await ctx.Challenges();
            Assert.Equal(authz.Challenges.Select(c => c.Url), challenges.Select(a => a.Location));

            // check the context returns empty list instead of null
            httpClientMock
                .Setup(m => m.Post<Authorization>(location, It.IsAny<JwsPayload>()))
                .ReturnsAsync(new AcmeHttpResponse<Authorization>(location, new Authorization(), default, default));
            challenges = await ctx.Challenges();
            Assert.Empty(challenges);
        }
    }
}

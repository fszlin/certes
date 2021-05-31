using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using Moq;
using Newtonsoft.Json;
using Xunit;
using static Certes.Acme.WellKnownServers;
using static Certes.Cli.CliTestHelper;
using static Certes.Helper;

using Authorization = Certes.Acme.Resource.Authorization;
using Challenge = Certes.Acme.Resource.Challenge;
using ChallengeTypes = Certes.Acme.Resource.ChallengeTypes;

namespace Certes.Cli.Commands
{
    public class OrderAuthzCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var domain = "www.abc.com";
            var orderLoc = new Uri("http://acme.com/o/1");

            var challenge1Loc = new Uri("http://acme.com/o/1/c/1");
            var challenge2Loc = new Uri("http://acme.com/o/1/c/2");
            var authzLoc = new Uri("http://acme.com/o/1/a/1");
            var authz = new Authorization
            {
                Identifier = new Identifier
                {
                    Type = IdentifierType.Dns,
                    Value = domain
                },
                Challenges = new[]
                {
                    new Challenge
                    {
                        Token = "http-token",
                        Type = ChallengeTypes.Http01,
                    },
                    new Challenge
                    {
                        Token = "dns-token",
                        Type = ChallengeTypes.Dns01,
                    }
                }                
            };
            var http01KeyAuthzThumbprint = "keyAuthz";

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(LetsEncryptV2);
            settingsMock.Setup(m => m.GetAccountKey(LetsEncryptV2)).ReturnsAsync(GetKeyV2());

            var challengeMock1 = new Mock<IChallengeContext>(MockBehavior.Strict);
            challengeMock1.SetupGet(m => m.Location).Returns(challenge1Loc);
            challengeMock1.SetupGet(m => m.Type).Returns(ChallengeTypes.Http01);
            challengeMock1.SetupGet(m => m.KeyAuthz).Returns(http01KeyAuthzThumbprint);
            challengeMock1.Setup(m => m.Resource()).ReturnsAsync(authz.Challenges[0]);
            var challengeMock2 = new Mock<IChallengeContext>(MockBehavior.Strict);
            challengeMock2.SetupGet(m => m.Location).Returns(challenge2Loc);
            challengeMock2.SetupGet(m => m.Type).Returns(ChallengeTypes.Dns01);
            challengeMock2.Setup(m => m.Resource()).ReturnsAsync(authz.Challenges[1]);

            var authzMock = new Mock<IAuthorizationContext>(MockBehavior.Strict);
            authzMock.Setup(m => m.Resource()).ReturnsAsync(authz);
            authzMock.Setup(m => m.Challenges())
                .ReturnsAsync(new[] { challengeMock1.Object, challengeMock2.Object });

            var orderMock = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock.Setup(m => m.Authorizations()).ReturnsAsync(new[] { authzMock.Object });

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);
            ctxMock.Setup(m => m.Order(orderLoc)).Returns(orderMock.Object);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);

            var (console, stdOutput, errOutput) = MockConsole();

            var cmd = new OrderAuthzCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object);
            var command = cmd.Define();

            await command.InvokeAsync($"authz {orderLoc} {domain} http", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            dynamic ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = challenge1Loc,
                    challengeFile = $".well-known/acme-challenge/{authz.Challenges[0].Token}",
                    challengeTxt = $"{authz.Challenges[0].Token}.{GetKeyV2().Thumbprint()}",
                    resource = authz.Challenges[0],
                    keyAuthz = http01KeyAuthzThumbprint
                }, JsonSettings),
                JsonConvert.SerializeObject(ret, JsonSettings));

            errOutput.Clear();
            stdOutput.Clear();

            await command.InvokeAsync($"authz {orderLoc} {domain} dns", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = challenge2Loc,
                    dnsTxt = GetKeyV2().DnsTxt(authz.Challenges[1].Token),
                    resource = authz.Challenges[1],
                }, JsonSettings),
                JsonConvert.SerializeObject(ret, JsonSettings));

            errOutput.Clear();
            stdOutput.Clear();

            await command.InvokeAsync($"authz {orderLoc} {domain} tls-sni", console.Object);
            Assert.False(errOutput.Length == 0, "Should print error");

            errOutput.Clear();
            stdOutput.Clear();

            await command.InvokeAsync($"authz {orderLoc} www.some.com http", console.Object);
            Assert.False(errOutput.Length == 0, "Should print error");

            authzMock.Setup(m => m.Challenges())
                .ReturnsAsync(new[] { challengeMock2.Object });
            errOutput.Clear();
            stdOutput.Clear();

            await command.InvokeAsync($"authz {orderLoc} {domain} http", console.Object);
            Assert.False(errOutput.Length == 0, "Should print error");
        }
    }
}

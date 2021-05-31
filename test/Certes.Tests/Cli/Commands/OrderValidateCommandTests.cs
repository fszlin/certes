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

namespace Certes.Cli.Commands
{
    public class OrderValidateCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var domain = "www.abc.com";
            var orderLoc = new Uri("http://acme.com/o/1");

            var challengeLoc = new Uri("http://acme.com/o/1/c/2");
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
                        Token = "dns-token",
                        Type = ChallengeTypes.Dns01,
                    }
                }
            };

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(LetsEncryptV2);
            settingsMock.Setup(m => m.GetAccountKey(LetsEncryptV2)).ReturnsAsync(GetKeyV2());

            var challengeMock = new Mock<IChallengeContext>(MockBehavior.Strict);
            challengeMock.SetupGet(m => m.Location).Returns(challengeLoc);
            challengeMock.SetupGet(m => m.Type).Returns(ChallengeTypes.Dns01);
            challengeMock.Setup(m => m.Validate()).ReturnsAsync(authz.Challenges[0]);

            var authzMock = new Mock<IAuthorizationContext>(MockBehavior.Strict);
            authzMock.Setup(m => m.Resource()).ReturnsAsync(authz);
            authzMock.Setup(m => m.Challenges())
                .ReturnsAsync(new[] { challengeMock.Object });

            var orderMock = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock.Setup(m => m.Authorizations()).ReturnsAsync(new[] { authzMock.Object });

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);
            ctxMock.Setup(m => m.Order(orderLoc)).Returns(orderMock.Object);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);

            var (console, stdOutput, errOutput) = MockConsole();

            var cmd = new OrderValidateCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object);
            var command = cmd.Define();

            await command.InvokeAsync($"validate {orderLoc} {domain} dns", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            var ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = challengeLoc,
                    resource = authz.Challenges[0],
                }, JsonSettings),
                JsonConvert.SerializeObject(ret, JsonSettings));

            challengeMock.Verify(m => m.Validate(), Times.Once);

            errOutput.Clear();
            stdOutput.Clear();

            // challenge type not supported
            await command.InvokeAsync($"validate {orderLoc} {domain} tls-sni", console.Object);
            Assert.False(errOutput.Length == 0, "Should print error");

            errOutput.Clear();
            stdOutput.Clear();

            // challenge not found
            await command.InvokeAsync($"validate {orderLoc} {domain} http", console.Object);
            Assert.False(errOutput.Length == 0, "Should print error");

            errOutput.Clear();
            stdOutput.Clear();

            await command.InvokeAsync($"validate {orderLoc} www.some.com http", console.Object);
            Assert.False(errOutput.Length == 0, "Should print error");
        }
    }
}

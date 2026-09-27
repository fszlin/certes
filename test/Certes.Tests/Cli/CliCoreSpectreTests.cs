using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using Moq;
using Xunit;

namespace Certes.Cli
{
    public class CliCoreSpectreTests
    {
        [Fact]
        public async Task AccountNewWithOutPathUsesFileUtilWriter()
        {
            var serverUri = new Uri("https://example.com/directory");
            var accountUri = new Uri("https://example.com/acme/acct/1");

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.WriteAllText("out.pem", It.IsAny<string>())).Returns(Task.CompletedTask);

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);

            var accountCtxMock = new Mock<IAccountContext>(MockBehavior.Strict);
            accountCtxMock.SetupGet(m => m.Location).Returns(accountUri);
            accountCtxMock.Setup(m => m.Resource()).ReturnsAsync(new Account
            {
                Status = AccountStatus.Valid,
                Contact = new List<string> { "mailto:test@example.com" },
            });

            var acmeMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            acmeMock
                .Setup(m => m.NewAccount(It.IsAny<IList<string>>(), true, null, null, null))
                .ReturnsAsync(accountCtxMock.Object);

            var envMock = new Mock<IEnvironmentVariables>(MockBehavior.Strict);

            var cli = new CliCoreSpectre(
                settingsMock.Object,
                (u, k) => acmeMock.Object,
                fileMock.Object,
                envMock.Object);

            var succeed = await cli.Run(new[]
            {
                "account",
                "new",
                "test@example.com",
                "--server",
                serverUri.ToString(),
                "--out-path",
                "out.pem",
            });

            Assert.True(succeed);
            fileMock.Verify(m => m.WriteAllText("out.pem", It.IsAny<string>()), Times.Once);
            settingsMock.Verify(m => m.SetAccountKey(It.IsAny<Uri>(), It.IsAny<IKey>()), Times.Never);
        }

        [Theory]
        [InlineData("authz")]
        [InlineData("validate")]
        public async Task OrderCommandsFindIpAuthorizations(string command)
        {
            var serverUri = new Uri("https://example.com/directory");
            var orderUri = new Uri("https://example.com/acme/order/1");
            var challengeUri = new Uri("https://example.com/acme/chall/1");
            var key = KeyFactory.NewKey(KeyAlgorithm.ES256);

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(serverUri);
            settingsMock.Setup(m => m.GetAccountKey(serverUri)).ReturnsAsync(key);

            var challengeMock = new Mock<IChallengeContext>(MockBehavior.Strict);
            challengeMock.SetupGet(m => m.Type).Returns(ChallengeTypes.Http01);
            challengeMock.SetupGet(m => m.Location).Returns(challengeUri);
            challengeMock.SetupGet(m => m.KeyAuthz).Returns("token.thumbprint");
            challengeMock.Setup(m => m.Resource()).ReturnsAsync(new Challenge { Type = ChallengeTypes.Http01, Token = "token" });
            challengeMock.Setup(m => m.Validate()).ReturnsAsync(new Challenge { Type = ChallengeTypes.Http01, Token = "token" });

            var authzMock = new Mock<IAuthorizationContext>(MockBehavior.Strict);
            authzMock.Setup(m => m.Resource()).ReturnsAsync(new Authorization
            {
                Identifier = new Identifier { Type = IdentifierType.Ip, Value = "2001:db8::1" },
            });
            authzMock.Setup(m => m.Challenges()).ReturnsAsync(new[] { challengeMock.Object });

            var orderMock = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock.Setup(m => m.Authorizations()).ReturnsAsync(new[] { authzMock.Object });

            var acmeMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            acmeMock.Setup(m => m.Order(orderUri)).Returns(orderMock.Object);

            var cli = new CliCoreSpectre(
                settingsMock.Object,
                (u, k) => acmeMock.Object,
                new Mock<IFileUtil>(MockBehavior.Strict).Object,
                new Mock<IEnvironmentVariables>(MockBehavior.Strict).Object);

            // Non-canonical spelling of the server's canonical address.
            var succeed = await cli.Run(new[] { "order", command, orderUri.ToString(), "2001:DB8:0::1", "http" });

            Assert.True(succeed);
            if (command == "validate")
            {
                challengeMock.Verify(m => m.Validate(), Times.Once);
            }
        }
    }
}

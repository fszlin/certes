using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Commands;
using Certes.Cli.Settings;
using Moq;
using Xunit;

namespace Certes.Cli
{
    public class CliCoreTests
    {
        [Fact]
        public async Task CanRunCommand()
        {
            var serverUri = new Uri("http://acme.com/d");
            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.SetDefaultServer(serverUri)).Returns(Task.CompletedTask);

            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(Helper.MockDirectoryV2);

            var cli = new CliCore(new[]
            {
                new ServerSetCommand(settingsMock.Object, (u, k) => ctxMock.Object)
            });

            var succeed = await cli.Run(new[] { "server", "set", $"{serverUri}" });
            Assert.True(succeed);
        }

        [Fact]
        public async Task CanShowHelpForGroup()
        {
            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);

            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(Helper.MockDirectoryV2);

            var cli = new CliCore(new[]
            {
                new ServerSetCommand(settingsMock.Object, (u, k) => ctxMock.Object)
            });

            Assert.True(await cli.Run(new[] { "-h" }));
        }

        [Fact]
        public async Task CanShowHelpForCommand()
        {
            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);

            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(Helper.MockDirectoryV2);

            var cli = new CliCore(new[]
            {
                new ServerSetCommand(settingsMock.Object, (u, k) => ctxMock.Object)
            });

            Assert.True(await cli.Run(new[] { "server", "-h" }));
            Assert.True(await cli.Run(new[] { "server", "set", "-h" }));
        }

        [Fact]
        public async Task InvalidCommand()
        {
            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);

            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(Helper.MockDirectoryV2);

            var cli = new CliCore(new[]
            {
                new ServerSetCommand(settingsMock.Object, (u, k) => ctxMock.Object)
            });

            Assert.False(await cli.Run(new string[0]));
            Assert.False(await cli.Run(new[] { "server", "ok" }));
        }

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
                Array.Empty<ICliCommand>(),
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
    }
}

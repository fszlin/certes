using System;
using System.Threading.Tasks;
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
    }
}

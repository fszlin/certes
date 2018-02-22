using System;
using System.Threading.Tasks;
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

            var cli = new CliCore
            {
                Settings = settingsMock.Object
            };

            var succeed = await cli.Run(new[] { "server", "set", "--server", $"{serverUri}" });
            Assert.True(succeed);

        }

        [Fact]
        public async Task CanShowHelpForGroup()
        {
            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);

            var cli = new CliCore
            {
                Settings = settingsMock.Object
            };

            Assert.False(await cli.Run(new[] { "-h" }));
        }

        [Fact]
        public async Task CanShowHelpForCommand()
        {
            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);

            var cli = new CliCore
            {
                Settings = settingsMock.Object
            };
            
            Assert.False(await cli.Run(new[] { "server", "-h" }));
        }

        [Fact]
        public async Task InvalidCommand()
        {
            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);

            var cli = new CliCore
            {
                Settings = settingsMock.Object
            };

            Assert.False(await cli.Run(new string[0]));
        }
    }
}

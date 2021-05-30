using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Moq;
using Newtonsoft.Json;
using Xunit;
using static Certes.Cli.CliTestHelper;
using static Certes.Helper;

namespace Certes.Cli.Commands
{
    public class ServerSetCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var serverUri = new Uri("http://acme.com/d");

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.SetDefaultServer(serverUri)).Returns(Task.CompletedTask);

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);

            var (console, stdOutput, errOutput) = MockConsole();

            var cmd = new ServerSetCommand(
                settingsMock.Object, (u, k) => ctxMock.Object);
            var command = cmd.Define();

            await command.InvokeAsync($"set {serverUri}", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            var ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(ret, JsonSettings),
                JsonConvert.SerializeObject(new
                {
                    location = serverUri,
                    resource = MockDirectoryV2
                }, JsonSettings));

            errOutput.Clear();
            stdOutput.Clear();

            await command.InvokeAsync($"set", console.Object);
            Assert.False(errOutput.Length == 0, "Should print error");
        }
    }
}

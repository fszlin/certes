using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Moq;
using Newtonsoft.Json;
using Xunit;
using static Certes.Acme.WellKnownServers;
using static Certes.Cli.CliTestHelper;
using static Certes.Helper;

namespace Certes.Cli.Commands
{
    public class ServerShowCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var serverUri = new Uri("http://acme.com/d");

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(LetsEncryptV2);

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);

            var (console, stdOutput, errOutput) = MockConsole();

            var cmd = new ServerShowCommand(
                settingsMock.Object, (u, k) => ctxMock.Object);
            var command = cmd.Define();

            await command.InvokeAsync($"show --server {serverUri}", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            var ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = serverUri,
                    resource = MockDirectoryV2
                }, JsonSettings),
                JsonConvert.SerializeObject(ret, JsonSettings));

            errOutput.Clear();
            stdOutput.Clear();

            await command.InvokeAsync($"show", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = LetsEncryptV2,
                    resource = MockDirectoryV2
                }, JsonSettings),
                JsonConvert.SerializeObject(ret, JsonSettings));
        }
    }
}

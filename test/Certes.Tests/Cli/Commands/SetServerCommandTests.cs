using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Moq;
using Newtonsoft.Json;
using Xunit;

using static Certes.Acme.WellKnownServers;
using static Certes.Helper;

namespace Certes.Cli.Commands
{
    public class SetServerCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var serverUri = new Uri("http://acme.com/d");

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.SetServer(serverUri)).Returns(Task.CompletedTask);

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);

            var cmd = new SetServerCommand(settingsMock.Object, (l, k) => ctxMock.Object);
            DefineCommand($"set-server {serverUri}", cmd: cmd);

            var ret = await cmd.Execute();
            Assert.Equal(
                JsonConvert.SerializeObject(ret),
                JsonConvert.SerializeObject(new
                {
                    location = serverUri,
                    directory = MockDirectoryV2
                }));
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"set-server {LetsEncryptStagingV2}";
            var cmd = DefineCommand(args);
            Assert.Equal(LetsEncryptStagingV2, cmd.ServerUri);

            DefineCommand("noop", false);

            Assert.Throws<ArgumentSyntaxException>(
                () => DefineCommand("set-server"));
        }

        private static SetServerCommand DefineCommand(string args, bool canProcess = true, SetServerCommand cmd = null)
        {
            if (cmd == null)
            {
                cmd = new SetServerCommand(new UserSettings());
            }

            ArgumentSyntax.Parse(args.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                Assert.Equal(canProcess, cmd.Define(syntax));
            });

            return cmd;
        }
    }
}

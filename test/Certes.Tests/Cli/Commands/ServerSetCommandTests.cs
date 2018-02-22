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

            var cmd = new ServerSetCommand(settingsMock.Object, (l, k) => ctxMock.Object);
            var syntax = DefineCommand($"set --server {serverUri}");

            var ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(ret),
                JsonConvert.SerializeObject(new
                {
                    location = serverUri,
                    directory = MockDirectoryV2
                }));

            syntax = DefineCommand($"set");
            await Assert.ThrowsAsync<ArgumentSyntaxException>(() => cmd.Execute(syntax));
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"set --server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("set", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);

            syntax = DefineCommand("noop");
            Assert.NotEqual("set", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new ServerSetCommand(new UserSettings());
            return ArgumentSyntax.Parse(args.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                cmd.Define(syntax);
            });
        }
    }
}

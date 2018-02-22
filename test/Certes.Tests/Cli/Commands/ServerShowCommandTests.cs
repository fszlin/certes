using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Acme;
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

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);

            var cmd = new ServerShowCommand(settingsMock.Object, (l, k) => ctxMock.Object);
            var syntax = DefineCommand($"show --server {serverUri}");

            var ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(ret),
                JsonConvert.SerializeObject(new
                {
                    location = serverUri,
                    directory = MockDirectoryV2
                }));

            syntax = DefineCommand($"show");

            ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(ret),
                JsonConvert.SerializeObject(new
                {
                    location = WellKnownServers.LetsEncryptV2,
                    directory = MockDirectoryV2
                }));
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"show --server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("show", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);

            syntax = DefineCommand("noop");
            Assert.NotEqual("show", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new ServerShowCommand(new UserSettings());
            return ArgumentSyntax.Parse(args.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                cmd.Define(syntax);
            });
        }
    }
}

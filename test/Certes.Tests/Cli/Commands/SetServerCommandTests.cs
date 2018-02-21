using System;
using System.CommandLine;
using System.Linq;
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
            var syntax = DefineCommand($"set-server {serverUri}");

            var ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(ret),
                JsonConvert.SerializeObject(new
                {
                    location = serverUri,
                    directory = MockDirectoryV2
                }));

            syntax = DefineCommand($"set-server");
            await Assert.ThrowsAsync<ArgumentSyntaxException>(() => cmd.Execute(syntax));
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"set-server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("set-server", syntax.ActiveCommand.Value);
            ValidateParameter(syntax, "server-uri", LetsEncryptStagingV2);

            syntax = DefineCommand("noop");
            Assert.NotEqual("set-server", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new SetServerCommand(new UserSettings());
            return ArgumentSyntax.Parse(args.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                cmd.Define(syntax);
            });
        }

        private static void ValidateParameter<T>(ArgumentSyntax syntax, string name, T value)
        {
            var arg = syntax.GetActiveParameters()
                .Where(p => p.Name == name)
                .OfType<Argument<T>>()
                .FirstOrDefault();
            Assert.NotNull(arg);
            Assert.Equal(value, arg.Value);
        }
    }
}

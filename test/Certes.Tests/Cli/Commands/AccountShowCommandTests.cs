using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using Moq;
using Newtonsoft.Json;
using Xunit;
using static Certes.Acme.WellKnownServers;
using static Certes.Cli.CliTestHelper;
using static Certes.Helper;

namespace Certes.Cli.Commands
{
    public class AccountShowCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var serverUri = new Uri("http://acme.com/d");
            var acctLoc = new Uri("http://acme.com/a/12");
            var keyPath = "./my-key.pem";
            var acct = new Account
            {
                Status = AccountStatus.Valid
            };

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(serverUri);
            settingsMock.Setup(m => m.GetAccountKey(serverUri))
                .ReturnsAsync(GetKeyV2());

            var acctCtxMock = new Mock<IAccountContext>(MockBehavior.Strict);
            acctCtxMock.SetupGet(m => m.Location).Returns(acctLoc);
            acctCtxMock.Setup(m => m.Resource()).ReturnsAsync(acct);

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);
            ctxMock.Setup(m => m.Account())
                .ReturnsAsync(acctCtxMock.Object);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.ReadAllText(It.IsAny<string>()))
                .ReturnsAsync(KeyAlgorithm.ES256.GetTestKey());

            var cmd = new AccountShowCommand(
                settingsMock.Object, MakeFactory(ctxMock), fileMock.Object);

            var syntax = DefineCommand($"show");
            var ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = acctLoc,
                    resource = acct
                }),
                JsonConvert.SerializeObject(ret));

            fileMock.ResetCalls();

            syntax = DefineCommand($"show --key {keyPath} --server {serverUri}");
            ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = acctLoc,
                    resource = acct
                }),
                JsonConvert.SerializeObject(ret));
            fileMock.Verify(m => m.ReadAllText(keyPath), Times.Once);

            settingsMock.Setup(m => m.GetAccountKey(serverUri)).ReturnsAsync((IKey)null);
            syntax = DefineCommand($"show");
            await Assert.ThrowsAsync<Exception>(() => cmd.Execute(syntax));
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
            var cmd = new AccountShowCommand(
                new UserSettings(new FileUtil()), MakeFactory(new Mock<IAcmeContext>()), new FileUtil());
            Assert.Equal(CommandGroup.Account.Command, cmd.Group.Command);
            return ArgumentSyntax.Parse(args.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                cmd.Define(syntax);
            });
        }
    }
}

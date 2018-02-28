using System;
using System.Collections.Generic;
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
    public class AccountSetCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var serverUri = new Uri("http://acme.com/d");
            var acctLoc = new Uri("http://acme.com/a/11");
            var keyPath = "./my-key.pem";
            var acct = new Account
            {
                Status = AccountStatus.Valid
            };

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(LetsEncryptV2);
            settingsMock.Setup(m => m.SetAccountKey(serverUri, It.IsAny<IKey>()))
                .Returns(Task.CompletedTask);

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

            var cmd = new AccountSetCommand(
                settingsMock.Object, MakeFactory(ctxMock), fileMock.Object);

            var syntax = DefineCommand($"set {keyPath} --server {serverUri}");
            var ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = acctLoc,
                    resource = acct
                }),
                JsonConvert.SerializeObject(ret));

            fileMock.Verify(m => m.ReadAllText(keyPath), Times.Once);
            settingsMock.Verify(m => m.SetAccountKey(serverUri, It.IsAny<IKey>()), Times.Once);
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"set ./acct-key.pem --server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("set", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);
            ValidateParameter(syntax, "key", "./acct-key.pem");

            syntax = DefineCommand("noop");
            Assert.NotEqual("new", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new AccountSetCommand(
                new UserSettings(new FileUtilImpl()), MakeFactory(new Mock<IAcmeContext>()), new FileUtilImpl());
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

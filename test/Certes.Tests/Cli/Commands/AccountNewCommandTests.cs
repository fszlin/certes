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
    public class AccountNewCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var serverUri = new Uri("http://acme.com/d");
            var email = "abc@example.com";
            var acctLoc = new Uri("http://acme.com/a/11");
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
            ctxMock.Setup(m => m.NewAccount(It.IsAny<IList<string>>(), true))
                .ReturnsAsync(acctCtxMock.Object);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.WriteAllTexts(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var cmd = new AccountNewCommand(settingsMock.Object, MakeFactory(ctxMock), fileMock.Object);
            var syntax = DefineCommand($"new {email} --server {serverUri}");

            var ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = acctLoc,
                    resource = acct
                }),
                JsonConvert.SerializeObject(ret));

        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"new abc@example.com --server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("new", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);
            ValidateParameter(syntax, "email", "abc@example.com");

            syntax = DefineCommand("noop");
            Assert.NotEqual("new", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new AccountNewCommand(new UserSettings(), MakeFactory(null), new FileUtilImpl());
            return ArgumentSyntax.Parse(args.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                cmd.Define(syntax);
            });
        }
    }
}

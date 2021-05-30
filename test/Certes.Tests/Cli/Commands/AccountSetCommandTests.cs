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

            var (console, stdOutput, errOutput) = MockConsole();

            var cmd = new AccountSetCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object);
            var command = cmd.Define();

            await command.InvokeAsync($"set {keyPath} --server {serverUri}", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            var ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = acctLoc,
                    resource = acct
                }, JsonSettings),
                JsonConvert.SerializeObject(ret));

            fileMock.Verify(m => m.ReadAllText(keyPath), Times.Once);
            settingsMock.Verify(m => m.SetAccountKey(serverUri, It.IsAny<IKey>()), Times.Once);
        }
    }
}

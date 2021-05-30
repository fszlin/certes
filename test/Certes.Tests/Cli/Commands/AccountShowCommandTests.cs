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

            var (console, stdOutput, errOutput) = MockConsole();

            var cmd = new AccountShowCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object);
            var command = cmd.Define();

            await command.InvokeAsync($"show", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            var ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = acctLoc,
                    resource = acct
                }, JsonSettings),
                JsonConvert.SerializeObject(ret));

            fileMock.ResetCalls();

            errOutput.Clear();
            stdOutput.Clear();

            await command.InvokeAsync($"show --key {keyPath} --server {serverUri}", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = acctLoc,
                    resource = acct
                }, JsonSettings),
                JsonConvert.SerializeObject(ret));
            fileMock.Verify(m => m.ReadAllText(keyPath), Times.Once);

            errOutput.Clear();
            stdOutput.Clear();
            settingsMock.Setup(m => m.GetAccountKey(serverUri)).ReturnsAsync((IKey)null);

            await command.InvokeAsync($"show", console.Object);
            Assert.False(errOutput.Length == 0, "Should print error");

            errOutput.Clear();
            stdOutput.Clear();
        }
    }
}

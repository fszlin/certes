using System;
using System.CommandLine;
using System.Text;
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
    public class OrderFinalizeCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var orderLoc = new Uri("http://acme.com/o/1");
            var order = new Order
            {
                Identifiers = new[] {
                    new Identifier { Value = "*.a.com" },
                    new Identifier { Value = "*.b.com" },
                }
            };

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(LetsEncryptV2);
            settingsMock.Setup(m => m.GetAccountKey(LetsEncryptV2)).ReturnsAsync(GetKeyV2());

            var orderMock = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock.SetupGet(m => m.Location).Returns(orderLoc);
            orderMock.Setup(m => m.Resource()).ReturnsAsync(order);
            orderMock.Setup(m => m.Finalize(It.IsAny<byte[]>())).ReturnsAsync(order);

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);
            ctxMock.Setup(m => m.Order(orderLoc)).Returns(orderMock.Object);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);

            var envMock = new Mock<IEnvironmentVariables>(MockBehavior.Strict);
            envMock.Setup(m => m.GetVar(It.IsAny<string>())).Returns((string)null);

            var (console, stdOutput, errOutput) = MockConsole();

            var cmd = new OrderFinalizeCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object, envMock.Object);
            var command = cmd.Define();

            await command.InvokeAsync($"finalize {orderLoc}", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            dynamic ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            var privateKeyBytes = Convert.FromBase64String($"{ret.privateKey}");
            var privateKey = KeyFactory.FromDer(privateKeyBytes);
            Assert.NotNull(privateKey);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = orderLoc,
                    resource = order,
                }, JsonSettings),
                JsonConvert.SerializeObject(new
                {
                    ret.location,
                    ret.resource,
                }, JsonSettings));

            orderMock.Verify(m => m.Finalize(It.IsAny<byte[]>()), Times.Once);

            var outPath = "./private-key.pem";
            orderMock.ResetCalls();
            fileMock.Setup(m => m.WriteAllText(outPath, It.IsAny<string>())).Returns(Task.CompletedTask);

            errOutput.Clear();
            stdOutput.Clear();

            await command.InvokeAsync($"finalize {orderLoc} --dn CN=*.a.com --out {outPath}", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = orderLoc,
                    resource = order,
                }, JsonSettings),
                JsonConvert.SerializeObject(ret, JsonSettings));

            fileMock.Verify(m => m.WriteAllText(outPath, It.IsAny<string>()), Times.Once);
            orderMock.Verify(m => m.Finalize(It.IsAny<byte[]>()), Times.Once);

            var keyPath = "./private-key.pem";
            orderMock.ResetCalls();
            fileMock.Setup(m => m.ReadAllText(keyPath)).ReturnsAsync(GetKeyV2().ToPem());
            errOutput.Clear();
            stdOutput.Clear();

            await command.InvokeAsync($"finalize {orderLoc} --dn CN=*.a.com --private-key {keyPath}", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = orderLoc,
                    resource = order,
                }, JsonSettings),
                JsonConvert.SerializeObject(ret, JsonSettings));

            orderMock.Verify(m => m.Finalize(It.IsAny<byte[]>()), Times.Once);
        }
    }
}

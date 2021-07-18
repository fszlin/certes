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
    public class OrderListCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var order1Loc = new Uri("http://acme.com/o/1");
            var order2Loc = new Uri("http://acme.com/o/2");
            var order1 = new Order
            {
                Identifiers = new[] {
                    new Identifier { Value = "*.a.com" },
                    new Identifier { Value = "*.b.com" },
                }
            };
            var order2 = new Order
            {
                Identifiers = new[] {
                    new Identifier { Value = "*.c.com" },
                }
            };

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(LetsEncryptV2);
            settingsMock.Setup(m => m.GetAccountKey(LetsEncryptV2)).ReturnsAsync(GetKeyV2());

            var orderMock1 = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock1.SetupGet(m => m.Location).Returns(order1Loc);
            orderMock1.Setup(m => m.Resource()).ReturnsAsync(order1);
            var orderMock2 = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock2.SetupGet(m => m.Location).Returns(order2Loc);
            orderMock2.Setup(m => m.Resource()).ReturnsAsync(order2);

            var orderListMock = new Mock<IOrderListContext>(MockBehavior.Strict);
            orderListMock.Setup(m => m.Orders()).ReturnsAsync(new[] { orderMock1.Object, orderMock2.Object });

            var acctCtxMock = new Mock<IAccountContext>(MockBehavior.Strict);
            acctCtxMock.Setup(m => m.Orders()).ReturnsAsync(orderListMock.Object);

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);
            ctxMock.Setup(m => m.Account()).ReturnsAsync(acctCtxMock.Object);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);

            var (console, stdOutput, errOutput) = MockConsole();

            var cmd = new OrderListCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object);
            var command = cmd.Define();

            await command.InvokeAsync($"list", console.Object);
            Assert.True(errOutput.Length == 0, errOutput.ToString());
            dynamic ret = JsonConvert.DeserializeObject(stdOutput.ToString());
            Assert.Equal(
                JsonConvert.SerializeObject(new[] 
                { 
                    new
                    {
                        location = order1Loc,
                        resource = order1,
                    },
                    new
                    {
                        location = order2Loc,
                        resource = order2,
                    }
                }, JsonSettings),
                JsonConvert.SerializeObject(ret, JsonSettings));
        }
    }
}

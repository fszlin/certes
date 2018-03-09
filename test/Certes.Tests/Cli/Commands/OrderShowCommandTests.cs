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
    public class OrderShowCommandTests
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

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);
            ctxMock.Setup(m => m.Order(orderLoc)).Returns(orderMock.Object);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);

            var cmd = new OrderShowCommand(
                settingsMock.Object, MakeFactory(ctxMock), fileMock.Object);

            var syntax = DefineCommand($"show {orderLoc}");
            var ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = orderLoc,
                    resource = order,
                }),
                JsonConvert.SerializeObject(ret));

            ctxMock.Verify(m => m.Order(orderLoc), Times.Once);
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"show http://acme.com/o/1 --server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("show", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);
            ValidateParameter(syntax, "order-id", new Uri("http://acme.com/o/1"));

            syntax = DefineCommand("noop");
            Assert.NotEqual("show", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new OrderShowCommand(
                NoopSettings(), MakeFactory(new Mock<IAcmeContext>()), new FileUtil());
            Assert.Equal(CommandGroup.Order.Command, cmd.Group.Command);
            return ArgumentSyntax.Parse(args.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                cmd.Define(syntax);
            });
        }
    }
}

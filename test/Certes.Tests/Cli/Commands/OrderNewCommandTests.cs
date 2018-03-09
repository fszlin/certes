using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
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
    public class OrderNewCommandTests
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
            ctxMock.Setup(m => m.NewOrder(It.IsAny<IList<string>>(), null, null))
                .ReturnsAsync(orderMock.Object);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);

            var cmd = new OrderNewCommand(
                settingsMock.Object, MakeFactory(ctxMock), fileMock.Object);

            var syntax = DefineCommand($"new a.com b.com");
            var ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = orderLoc,
                    resource = order,
                }),
                JsonConvert.SerializeObject(ret));

            ctxMock.Verify(m => m.NewOrder(new[] { "a.com", "b.com" }, null, null), Times.Once);
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"new www.abc1.com www.abc2.com --server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("new", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);
            var domains = syntax.GetActiveArguments().Single(a => a.Name == "domains");
            Assert.Equal(new[] { "www.abc1.com", "www.abc2.com" }, domains.Value);

            syntax = DefineCommand("noop");
            Assert.NotEqual("list", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new OrderNewCommand(
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

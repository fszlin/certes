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

            var cmd = new OrderFinalizeCommand(
                settingsMock.Object, MakeFactory(ctxMock), fileMock.Object);

            var syntax = DefineCommand($"finalize {orderLoc}");
            dynamic ret = await cmd.Execute(syntax);
            var privateKey = KeyFactory.FromDer(ret.privateKey);
            Assert.NotNull(privateKey);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = orderLoc,
                    resource = order,
                }),
                JsonConvert.SerializeObject(new
                {
                    ret.location,
                    ret.resource,
                }));

            orderMock.Verify(m => m.Finalize(It.IsAny<byte[]>()), Times.Once);

            var outPath = "./private-key.pem";
            orderMock.ResetCalls();
            fileMock.Setup(m => m.WriteAllText(outPath, It.IsAny<string>())).Returns(Task.CompletedTask);

            syntax = DefineCommand($"finalize {orderLoc} --dn CN=*.a.com --out {outPath}");
            ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = orderLoc,
                    resource = order,
                }),
                JsonConvert.SerializeObject(ret));

            fileMock.Verify(m => m.WriteAllText(outPath, It.IsAny<string>()), Times.Once);
            orderMock.Verify(m => m.Finalize(It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"finalize http://a.com/o/1 --dn CN=www.m.com --out ./p.pem --server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("finalize", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);
            ValidateOption(syntax, "dn", "CN=www.m.com");
            ValidateOption(syntax, "out", "./p.pem");
            ValidateParameter(syntax, "order-id", new Uri("http://a.com/o/1"));

            syntax = DefineCommand("noop");
            Assert.NotEqual("finalize", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new OrderFinalizeCommand(
                new UserSettings(new FileUtilImpl()), MakeFactory(new Mock<IAcmeContext>()), new FileUtilImpl());
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

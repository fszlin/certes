using System;
using System.CommandLine;
using System.IO;
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
    public class CertificatePfxCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var orderLoc = new Uri("http://acme.com/o/1");
            var certLoc = new Uri("http://acme.com/c/1");
            var privateKeyPath = "./my-key.pem";
            var order = new Order
            {
                Certificate = certLoc,
                Identifiers = new[] {
                    new Identifier { Value = "*.a.com" },
                    new Identifier { Value = "*.b.com" },
                },
                Status = OrderStatus.Valid,
            };

            var certChainContent = string.Join(
                Environment.NewLine,
                File.ReadAllText("./Data/leaf-cert.pem"),
                File.ReadAllText("./Data/test-ca2.pem"),
                File.ReadAllText("./Data/test-root.pem"));
            var certChain = new CertificateChain(certChainContent);

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(LetsEncryptV2);
            settingsMock.Setup(m => m.GetAccountKey(LetsEncryptV2)).ReturnsAsync(GetKeyV2());

            var orderMock = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock.Setup(m => m.Resource()).ReturnsAsync(order);
            orderMock.Setup(m => m.Download()).ReturnsAsync(certChain);

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);
            ctxMock.Setup(m => m.Order(orderLoc)).Returns(orderMock.Object);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.ReadAllText(privateKeyPath)).ReturnsAsync(KeyAlgorithm.RS256.GetTestKey());

            var cmd = new CertificatePfxCommand(
                settingsMock.Object, MakeFactory(ctxMock), fileMock.Object);

            var syntax = DefineCommand($"pfx {orderLoc} {privateKeyPath} abcd1234");
            dynamic ret = await cmd.Execute(syntax);
            Assert.Equal(certLoc, ret.location);
            Assert.NotNull(ret.pfx);

            orderMock.Verify(m => m.Download(), Times.Once);

            var outPath = "./cert.pfx";
            fileMock.Setup(m => m.WriteAllBytes(outPath, It.IsAny<byte[]>()))
                .Returns(Task.CompletedTask);
            syntax = DefineCommand($"pfx {orderLoc} {privateKeyPath} abcd1234 --out {outPath}");
            ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = certLoc,
                }),
                JsonConvert.SerializeObject(ret));

            fileMock.Verify(m => m.WriteAllBytes(outPath, It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"pfx http://acme.com/o/1 ./my-key.pem abcd1234 --server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("pfx", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);
            ValidateParameter(syntax, "order-id", new Uri("http://acme.com/o/1"));
            ValidateParameter(syntax, "private-key", "./my-key.pem");
            ValidateParameter(syntax, "password", "abcd1234");

            syntax = DefineCommand("noop");
            Assert.NotEqual("pfx", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new CertificatePfxCommand(
                new UserSettings(new FileUtilImpl()), MakeFactory(new Mock<IAcmeContext>()), new FileUtilImpl());
            Assert.Equal(CommandGroup.Certificate.Command, cmd.Group.Command);
            return ArgumentSyntax.Parse(args.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                cmd.Define(syntax);
            });
        }
    }
}

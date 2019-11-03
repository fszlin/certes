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
    public class CertificatePemCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var orderLoc = new Uri("http://acme.com/o/1");
            var certLoc = new Uri("http://acme.com/c/1");
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
                File.ReadAllText("./Data/leaf-cert.pem").Trim(),
                File.ReadAllText("./Data/test-ca2.pem").Trim(),
                File.ReadAllText("./Data/test-root.pem").Trim());
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

            var cmd = new CertificatePemCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object);

            var syntax = DefineCommand($"pem {orderLoc}");
            var ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = certLoc,
                    resource = new
                    {
                        certificate = certChain.Certificate.ToDer(),
                        issuers = certChain.Issuers.Select(i => i.ToDer()),
                    },
                }),
                JsonConvert.SerializeObject(ret));

            orderMock.Verify(m => m.Download(), Times.Once);

            var outPath = "./cert.pem";
            string saved = null;
            fileMock.Setup(m => m.WriteAllText(outPath, It.IsAny<string>()))
                .Callback((string path, string text) => saved = text)
                .Returns(Task.CompletedTask);
            syntax = DefineCommand($"pem --out {outPath} {orderLoc}");
            ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = certLoc,
                }),
                JsonConvert.SerializeObject(ret));

            fileMock.Verify(m => m.WriteAllText(outPath, It.IsAny<string>()), Times.Once);
            Assert.Equal(
                certChainContent.Replace("\r", ""),
                saved.Replace("\r", "").TrimEnd());

            order.Status = OrderStatus.Invalid;
            syntax = DefineCommand($"pem {orderLoc}");
            await Assert.ThrowsAsync<CertesCliException>(() => cmd.Execute(syntax));
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"pem http://acme.com/o/1 --server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("pem", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);
            ValidateParameter(syntax, "order-id", new Uri("http://acme.com/o/1"));

            syntax = DefineCommand("noop");
            Assert.NotEqual("pem", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new CertificatePemCommand(
                NoopSettings(), (u, k) => new Mock<IAcmeContext>().Object, new FileUtil());
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

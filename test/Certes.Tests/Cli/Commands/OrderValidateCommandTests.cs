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
    public class OrderValidateCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var domain = "www.abc.com";
            var orderLoc = new Uri("http://acme.com/o/1");

            var challengeLoc = new Uri("http://acme.com/o/1/c/2");
            var authzLoc = new Uri("http://acme.com/o/1/a/1");
            var authz = new Authorization
            {
                Identifier = new Identifier
                {
                    Type = IdentifierType.Dns,
                    Value = domain
                },
                Challenges = new[]
                {
                    new Challenge
                    {
                        Token = "dns-token",
                        Type = ChallengeTypes.Dns01,
                    }
                }
            };

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(LetsEncryptV2);
            settingsMock.Setup(m => m.GetAccountKey(LetsEncryptV2)).ReturnsAsync(GetKeyV2());

            var challengeMock = new Mock<IChallengeContext>(MockBehavior.Strict);
            challengeMock.SetupGet(m => m.Location).Returns(challengeLoc);
            challengeMock.SetupGet(m => m.Type).Returns(ChallengeTypes.Dns01);
            challengeMock.Setup(m => m.Validate()).ReturnsAsync(authz.Challenges[0]);

            var authzMock = new Mock<IAuthorizationContext>(MockBehavior.Strict);
            authzMock.Setup(m => m.Resource()).ReturnsAsync(authz);
            authzMock.Setup(m => m.Challenges())
                .ReturnsAsync(new[] { challengeMock.Object });

            var orderMock = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock.Setup(m => m.Authorizations()).ReturnsAsync(new[] { authzMock.Object });

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);
            ctxMock.Setup(m => m.Order(orderLoc)).Returns(orderMock.Object);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);

            var cmd = new OrderValidateCommand(
                settingsMock.Object, (u, k) => ctxMock.Object, fileMock.Object);

            var syntax = DefineCommand($"validate {orderLoc} {domain} dns");
            var ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = challengeLoc,
                    resource = authz.Challenges[0],
                }),
                JsonConvert.SerializeObject(ret));

            challengeMock.Verify(m => m.Validate(), Times.Once);

            // challenge type not supported
            syntax = DefineCommand($"validate {orderLoc} {domain} tls-sni");
            await Assert.ThrowsAsync<ArgumentSyntaxException>(() => cmd.Execute(syntax));

            // challenge not found
            syntax = DefineCommand($"validate {orderLoc} {domain} http");
            await Assert.ThrowsAsync<Exception>(() => cmd.Execute(syntax));

            syntax = DefineCommand($"validate {orderLoc} www.some.com http");
            await Assert.ThrowsAsync<Exception>(() => cmd.Execute(syntax));
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"validate http://acme.com/o/1 www.abc.com http --server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("validate", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);
            ValidateParameter(syntax, "order-id", new Uri("http://acme.com/o/1"));
            ValidateParameter(syntax, "domain", "www.abc.com");
            ValidateParameter(syntax, "challenge-type", "http");

            syntax = DefineCommand("noop");
            Assert.NotEqual("validate", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new OrderValidateCommand(
                NoopSettings(), (u, k) => new Mock<IAcmeContext>().Object, new FileUtil());
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

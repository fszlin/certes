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

using Authorization = Certes.Acme.Resource.Authorization;
using Challenge = Certes.Acme.Resource.Challenge;
using ChallengeTypes = Certes.Acme.Resource.ChallengeTypes;

namespace Certes.Cli.Commands
{
    public class OrderAuthzCommandTests
    {
        [Fact]
        public async Task CanProcessCommand()
        {
            var domain = "www.abc.com";
            var orderLoc = new Uri("http://acme.com/o/1");

            var challenge1Loc = new Uri("http://acme.com/o/1/c/1");
            var challenge2Loc = new Uri("http://acme.com/o/1/c/2");
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
                        Token = "http-token",
                        Type = ChallengeTypes.Http01,
                    },
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

            var challengeMock1 = new Mock<IChallengeContext>(MockBehavior.Strict);
            challengeMock1.SetupGet(m => m.Location).Returns(challenge1Loc);
            challengeMock1.SetupGet(m => m.Type).Returns(ChallengeTypes.Http01);
            challengeMock1.Setup(m => m.Resource()).ReturnsAsync(authz.Challenges[0]);
            var challengeMock2 = new Mock<IChallengeContext>(MockBehavior.Strict);
            challengeMock2.SetupGet(m => m.Location).Returns(challenge2Loc);
            challengeMock2.SetupGet(m => m.Type).Returns(ChallengeTypes.Dns01);
            challengeMock2.Setup(m => m.Resource()).ReturnsAsync(authz.Challenges[1]);

            var authzMock = new Mock<IAuthorizationContext>(MockBehavior.Strict);
            authzMock.Setup(m => m.Resource()).ReturnsAsync(authz);
            authzMock.Setup(m => m.Challenges())
                .ReturnsAsync(new[] { challengeMock1.Object, challengeMock2.Object });

            var orderMock = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock.Setup(m => m.Authorizations()).ReturnsAsync(new[] { authzMock.Object });

            var ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctxMock.Setup(m => m.GetDirectory()).ReturnsAsync(MockDirectoryV2);
            ctxMock.Setup(m => m.Order(orderLoc)).Returns(orderMock.Object);

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);

            var cmd = new OrderAuthzCommand(
                settingsMock.Object, MakeFactory(ctxMock), fileMock.Object);

            var syntax = DefineCommand($"authz {orderLoc} {domain} http");
            var ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = challenge1Loc,
                    resource = authz.Challenges[0],
                }),
                JsonConvert.SerializeObject(ret));

            syntax = DefineCommand($"authz {orderLoc} {domain} dns");
            ret = await cmd.Execute(syntax);
            Assert.Equal(
                JsonConvert.SerializeObject(new
                {
                    location = challenge2Loc,
                    dnsTxt = GetKeyV2().DnsTxt(authz.Challenges[1].Token),
                    resource = authz.Challenges[1],
                }),
                JsonConvert.SerializeObject(ret));

            syntax = DefineCommand($"authz {orderLoc} {domain} tls-sni");
            await Assert.ThrowsAsync<ArgumentSyntaxException>(() => cmd.Execute(syntax));

            syntax = DefineCommand($"authz {orderLoc} www.some.com http");
            await Assert.ThrowsAsync<Exception>(() => cmd.Execute(syntax));

            authzMock.Setup(m => m.Challenges())
                .ReturnsAsync(new[] { challengeMock2.Object });

            syntax = DefineCommand($"authz {orderLoc} {domain} http");
            await Assert.ThrowsAsync<Exception>(() => cmd.Execute(syntax));
        }

        [Fact]
        public void CanDefineCommand()
        {
            var args = $"authz http://acme.com/o/1 www.abc.com http --server {LetsEncryptStagingV2}";
            var syntax = DefineCommand(args);

            Assert.Equal("authz", syntax.ActiveCommand.Value);
            ValidateOption(syntax, "server", LetsEncryptStagingV2);
            ValidateParameter(syntax, "order-id", new Uri("http://acme.com/o/1"));
            ValidateParameter(syntax, "domain", "www.abc.com");
            ValidateParameter(syntax, "challenge-type", "http");

            syntax = DefineCommand("noop");
            Assert.NotEqual("authz", syntax.ActiveCommand.Value);
        }

        private static ArgumentSyntax DefineCommand(string args)
        {
            var cmd = new OrderAuthzCommand(
                new UserSettings(new FileUtilImpl()), MakeFactory(new Mock<IAcmeContext>()), new FileUtilImpl());
            return ArgumentSyntax.Parse(args.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                cmd.Define(syntax);
            });
        }
    }
}

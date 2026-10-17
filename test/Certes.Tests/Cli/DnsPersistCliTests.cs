using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using Moq;
using Xunit;

namespace Certes.Cli
{
    [CollectionDefinition("Console output", DisableParallelization = true)]
    public class ConsoleOutputCollection { }

    [Collection("Console output")]
    public class DnsPersistCliTests
    {
        [Theory]
        [InlineData("dns-persist")]
        [InlineData("dns-persist-01")]
        public async Task OutputsRecordAndValidatesWithoutToken(string alias)
        {
            using var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var server = new Uri("https://ca.example/directory");
            var orderUrl = new Uri("https://ca.example/order/1");
            var accountUrl = new Uri("https://ca.example/acct/1");
            var key = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var settings = new Mock<IUserSettings>(MockBehavior.Strict);
            settings.Setup(s => s.GetDefaultServer(token)).ReturnsAsync(server);
            settings.Setup(s => s.GetAccountKey(server, token)).ReturnsAsync(key);
            var resource = new Challenge { Type = ChallengeTypes.DnsPersist01, IssuerDomainNames = new[] { "ca.example", "other.example" } };
            var challenge = new Mock<IChallengeContext>(MockBehavior.Strict);
            challenge.SetupGet(c => c.Type).Returns(ChallengeTypes.DnsPersist01);
            challenge.SetupGet(c => c.Location).Returns(new Uri("https://ca.example/chall/1"));
            challenge.Setup(c => c.Resource(token)).ReturnsAsync(resource);
            challenge.Setup(c => c.Validate(token)).ReturnsAsync(resource);
            var authorization = new Mock<IAuthorizationContext>(MockBehavior.Strict);
            authorization.Setup(a => a.Resource(token)).ReturnsAsync(new Authorization
            {
                Identifier = new Identifier { Type = IdentifierType.Dns, Value = "example.com" }, Wildcard = true,
            });
            authorization.Setup(a => a.Challenges(token)).ReturnsAsync(new[] { challenge.Object });
            var order = new Mock<IOrderContext>(MockBehavior.Strict);
            order.Setup(o => o.Authorizations(token)).ReturnsAsync(new[] { authorization.Object });
            var account = new Mock<IAccountContext>(MockBehavior.Strict);
            account.SetupGet(a => a.Location).Returns(accountUrl);
            var context = new Mock<IAcmeContext>(MockBehavior.Strict);
            context.Setup(c => c.Order(orderUrl)).Returns(order.Object);
            context.Setup(c => c.Account(token)).ReturnsAsync(account.Object);
            context.SetupGet(c => c.AccountKey).Returns(key);
            context.Setup(c => c.GetDirectory(token)).ReturnsAsync(new Acme.Resource.Directory(null, null, null, null, null,
                new DirectoryMeta(null, null, null, null, null, new[] { "ca.example" }, "https://ca.example/hash/"), null));
            var cli = new CliCoreSpectre(settings.Object, (u, k) => context.Object,
                new Mock<IFileUtil>(MockBehavior.Strict).Object, new Mock<IEnvironmentVariables>(MockBehavior.Strict).Object);
            var original = Console.Out;
            using var output = new StringWriter();
            try
            {
                Console.SetOut(output);
                var expires = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal(0, await cli.RunWithExitCode(new[] { "order", "authz", orderUrl.ToString(), "*.example.com", alias,
                    "--wildcard-policy", "--persist-until", expires, "--issuer-domain-name", "other.example" }, token));
                using var json = JsonDocument.Parse(output.ToString());
                Assert.Equal("_validation-persist.example.com", json.RootElement.GetProperty("dnsName").GetString());
                var value = json.RootElement.GetProperty("dnsTxt").GetString();
                Assert.StartsWith("other.example; accounturi=https://ca.example/hash/sha-256/", value);
                Assert.EndsWith("; policy=wildcard; persistUntil=" + expires, value);
                Assert.False(json.RootElement.TryGetProperty("keyAuthz", out _));
                Assert.False(json.RootElement.TryGetProperty("challengeFile", out _));
                Assert.Equal(value, json.RootElement.GetProperty("dnsTxtChunks")[0].GetString());
                Assert.Equal(0, await cli.RunWithExitCode(new[] { "order", "validate", orderUrl.ToString(), "*.example.com", alias }, token));
                challenge.Verify(c => c.Validate(token), Times.Once);
            }
            finally { Console.SetOut(original); }
        }

        [Theory]
        [InlineData("http", "--wildcard-policy", null)]
        [InlineData("dns", "--persist-until", "9999999999")]
        [InlineData("dns-persist", "--persist-until", "-1")]
        [InlineData("dns-persist", "--persist-until", "9223372036854775807")]
        public async Task RejectsInvalidOptionsBeforeAccountAccess(string type, string option, string value)
        {
            var settings = new Mock<IUserSettings>(MockBehavior.Strict);
            var cli = new CliCoreSpectre(settings.Object, (u, k) => throw new InvalidOperationException(),
                new Mock<IFileUtil>(MockBehavior.Strict).Object, new Mock<IEnvironmentVariables>(MockBehavior.Strict).Object);
            var args = new System.Collections.Generic.List<string> { "order", "authz", "https://ca.example/order/1", "example.com", type, option };
            if (value != null) { args.Add(value); }
            Assert.Equal(1, await cli.RunWithExitCode(args.ToArray()));
            settings.VerifyNoOtherCalls();
        }
    }
}

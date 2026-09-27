using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using Moq;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Certes.Crypto;
using Directory = Certes.Acme.Resource.Directory;
using Xunit;

namespace Certes.Cli
{
    public class CliCoreSpectreTests
    {
        [Fact]
        public async Task AccountNewWithOutPathUsesFileUtilWriter()
        {
            var serverUri = new Uri("https://example.com/directory");
            var accountUri = new Uri("https://example.com/acme/acct/1");

            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            fileMock.Setup(m => m.WriteAllText("out.pem", It.IsAny<string>())).Returns(Task.CompletedTask);

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);

            var accountCtxMock = new Mock<IAccountContext>(MockBehavior.Strict);
            accountCtxMock.SetupGet(m => m.Location).Returns(accountUri);
            accountCtxMock.Setup(m => m.Resource()).ReturnsAsync(new Account
            {
                Status = AccountStatus.Valid,
                Contact = new List<string> { "mailto:test@example.com" },
            });

            var acmeMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            acmeMock
                .Setup(m => m.NewAccount(It.IsAny<IList<string>>(), true, null, null, null))
                .ReturnsAsync(accountCtxMock.Object);

            var envMock = new Mock<IEnvironmentVariables>(MockBehavior.Strict);

            var cli = new CliCoreSpectre(
                settingsMock.Object,
                (u, k) => acmeMock.Object,
                fileMock.Object,
                envMock.Object);

            var succeed = await cli.Run(new[]
            {
                "account",
                "new",
                "test@example.com",
                "--server",
                serverUri.ToString(),
                "--out-path",
                "out.pem",
            });

            Assert.True(succeed);
            fileMock.Verify(m => m.WriteAllText("out.pem", It.IsAny<string>()), Times.Once);
            settingsMock.Verify(m => m.SetAccountKey(It.IsAny<Uri>(), It.IsAny<IKey>()), Times.Never);
        }

        [Theory]
        [InlineData("authz")]
        [InlineData("validate")]
        public async Task OrderCommandsFindIpAuthorizations(string command)
        {
            var serverUri = new Uri("https://example.com/directory");
            var orderUri = new Uri("https://example.com/acme/order/1");
            var challengeUri = new Uri("https://example.com/acme/chall/1");
            var key = KeyFactory.NewKey(KeyAlgorithm.ES256);

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(serverUri);
            settingsMock.Setup(m => m.GetAccountKey(serverUri)).ReturnsAsync(key);

            var challengeMock = new Mock<IChallengeContext>(MockBehavior.Strict);
            challengeMock.SetupGet(m => m.Type).Returns(ChallengeTypes.Http01);
            challengeMock.SetupGet(m => m.Location).Returns(challengeUri);
            challengeMock.SetupGet(m => m.KeyAuthz).Returns("token.thumbprint");
            challengeMock.Setup(m => m.Resource()).ReturnsAsync(new Challenge { Type = ChallengeTypes.Http01, Token = "token" });
            challengeMock.Setup(m => m.Validate()).ReturnsAsync(new Challenge { Type = ChallengeTypes.Http01, Token = "token" });

            var authzMock = new Mock<IAuthorizationContext>(MockBehavior.Strict);
            authzMock.Setup(m => m.Resource()).ReturnsAsync(new Authorization
            {
                Identifier = new Identifier { Type = IdentifierType.Ip, Value = "2001:db8::1" },
            });
            authzMock.Setup(m => m.Challenges()).ReturnsAsync(new[] { challengeMock.Object });

            var orderMock = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock.Setup(m => m.Authorizations()).ReturnsAsync(new[] { authzMock.Object });

            var acmeMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            acmeMock.Setup(m => m.Order(orderUri)).Returns(orderMock.Object);

            var cli = new CliCoreSpectre(
                settingsMock.Object,
                (u, k) => acmeMock.Object,
                new Mock<IFileUtil>(MockBehavior.Strict).Object,
                new Mock<IEnvironmentVariables>(MockBehavior.Strict).Object);

            // Non-canonical spelling of the server's canonical address.
            var succeed = await cli.Run(new[] { "order", command, orderUri.ToString(), "2001:DB8:0::1", "http" });

            Assert.True(succeed);
            if (command == "validate")
            {
                challengeMock.Verify(m => m.Validate(), Times.Once);
            }
        }

        [Theory]
        [InlineData(null, null, null, null)]
        [InlineData("shortlived", null, "shortlived", null)]
        [InlineData(null, "abc.AQ", null, "abc.AQ")]
        [InlineData("shortlived", "abc.AQ", "shortlived", "abc.AQ")]
        public async Task OrderNewSendsProfileAndReplacement(string profile, string replaces, string expectedProfile, string expectedReplaces)
        {
            var (acme, sent) = CreateOrderingContext();
            var args = new List<string> { "order", "new", "192.0.2.1", "www.example.com" };
            if (profile != null) { args.AddRange(new[] { "--profile", profile }); }
            if (replaces != null) { args.AddRange(new[] { "--replaces", replaces }); }

            var succeed = await CreateCli(acme).Run(args.ToArray());

            Assert.True(succeed);
            var order = Assert.Single(sent);
            Assert.Equal(expectedProfile, order.Profile);
            Assert.Equal(expectedReplaces, order.Replaces);
            Assert.Equal(new[] { (IdentifierType.Ip, "192.0.2.1"), (IdentifierType.Dns, "www.example.com") },
                order.Identifiers.Select(i => (i.Type, i.Value)));
        }

        [Theory]
        [InlineData("--profile", "")]
        [InlineData("--profile", " ")]
        [InlineData("--replaces", "")]
        [InlineData("--replaces", "\t")]
        public async Task OrderNewRejectsEmptyOptionValues(string option, string value)
        {
            var (acme, sent) = CreateOrderingContext();
            var succeed = await CreateCli(acme).Run(new[] { "order", "new", "www.example.com", option, value });
            Assert.False(succeed);
            Assert.Empty(sent);
        }

        [Fact]
        public async Task OrderNewRejectsUnadvertisedProfile()
        {
            var (acme, sent) = CreateOrderingContext();
            var succeed = await CreateCli(acme).Run(new[] { "order", "new", "www.example.com", "--profile", "tlsserver" });
            Assert.False(succeed);
            Assert.Empty(sent);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CertRenewalInfoPrintsWindowForCertificateFile(bool privateKeyFirst)
        {
            var serverUri = new Uri("https://example.com/directory");
            var directory = new Directory(null, null, null, null, null, null, new Uri("https://example.com/renewal-info"));
            var info = new RenewalInfo
            {
                SuggestedWindow = new SuggestedWindow
                {
                    Start = DateTimeOffset.Parse("2025-01-02T04:00:00Z"),
                    End = DateTimeOffset.Parse("2025-01-03T04:00:00Z"),
                },
            };
            var expectedUri = new Uri("https://example.com/renewal-info/aYhba4dGQEHhs3uEe6CuLN4ByNQ.AIdlQyE");
            var http = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            http.Setup(m => m.Get<RenewalInfo>(expectedUri))
                .ReturnsAsync(new AcmeHttpResponse<RenewalInfo>(null, info, null, null, 21600));
            var acmeMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            acmeMock.Setup(m => m.GetDirectory()).ReturnsAsync(directory);
            acmeMock.SetupGet(m => m.HttpClient).Returns(http.Object);

            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(serverUri);
            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);
            var pem = Rfc9773ExampleCertificatePem();
            if (privateKeyFirst)
            {
                // Some tools write the private key before the certificate in one PEM file.
                pem = KeyFactory.NewKey(KeyAlgorithm.ES256).ToPem() + pem;
            }

            fileMock.Setup(m => m.ReadAllText("cert.pem")).ReturnsAsync(pem);
            Uri requestedServer = null;
            var cli = new CliCoreSpectre(settingsMock.Object, (u, k) => { requestedServer = u; return acmeMock.Object; },
                fileMock.Object, new Mock<IEnvironmentVariables>(MockBehavior.Strict).Object);

            var succeed = await cli.Run(new[] { "cert", "renewal-info", "cert.pem" });

            Assert.True(succeed);
            Assert.Equal(serverUri, requestedServer);
            http.Verify(m => m.Get<RenewalInfo>(expectedUri), Times.Once);
        }

        [Theory]
        [InlineData("tls-alpn")]
        [InlineData("tls-alpn-01")]
        [InlineData("HTTP-01")]
        [InlineData("dns-01")]
        public async Task OrderAuthzAcceptsChallengeTypeAliases(string challengeType)
        {
            var expected = challengeType.StartsWith("tls", StringComparison.OrdinalIgnoreCase) ? ChallengeTypes.TlsAlpn01
                : challengeType.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? ChallengeTypes.Http01
                : ChallengeTypes.Dns01;
            var (cli, challengeMock, orderUri) = CreateAuthzCli(expected);

            var succeed = await cli.Run(new[] { "order", "authz", orderUri.ToString(), "www.example.com", challengeType });

            Assert.True(succeed);
            challengeMock.Verify(m => m.Resource(), Times.Once);
        }

        [Fact]
        public async Task OrderAuthzRejectsUnknownChallengeType()
        {
            var (cli, challengeMock, orderUri) = CreateAuthzCli(ChallengeTypes.Http01);
            Assert.False(await cli.Run(new[] { "order", "authz", orderUri.ToString(), "www.example.com", "email" }));
            challengeMock.Verify(m => m.Resource(), Times.Never);
        }

        private static (CliCoreSpectre, Mock<IChallengeContext>, Uri) CreateAuthzCli(string challengeType)
        {
            var serverUri = new Uri("https://example.com/directory");
            var orderUri = new Uri("https://example.com/acme/order/1");
            var key = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(serverUri);
            settingsMock.Setup(m => m.GetAccountKey(serverUri)).ReturnsAsync(key);

            var challengeMock = new Mock<IChallengeContext>(MockBehavior.Strict);
            challengeMock.SetupGet(m => m.Type).Returns(challengeType);
            challengeMock.SetupGet(m => m.Location).Returns(new Uri("https://example.com/acme/chall/1"));
            challengeMock.SetupGet(m => m.KeyAuthz).Returns("token.thumbprint");
            challengeMock.Setup(m => m.Resource()).ReturnsAsync(new Challenge { Type = challengeType, Token = "token" });

            var authzMock = new Mock<IAuthorizationContext>(MockBehavior.Strict);
            authzMock.Setup(m => m.Resource()).ReturnsAsync(new Authorization
            {
                Identifier = new Identifier { Type = IdentifierType.Dns, Value = "www.example.com" },
            });
            authzMock.Setup(m => m.Challenges()).ReturnsAsync(new[] { challengeMock.Object });
            var orderMock = new Mock<IOrderContext>(MockBehavior.Strict);
            orderMock.Setup(m => m.Authorizations()).ReturnsAsync(new[] { authzMock.Object });
            var acmeMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            acmeMock.Setup(m => m.Order(orderUri)).Returns(orderMock.Object);

            var cli = new CliCoreSpectre(settingsMock.Object, (u, k) => acmeMock.Object,
                new Mock<IFileUtil>(MockBehavior.Strict).Object, new Mock<IEnvironmentVariables>(MockBehavior.Strict).Object);
            return (cli, challengeMock, orderUri);
        }

        // A real AcmeContext over a mocked transport, so library behavior (IP detection,
        // profile validation) is exercised. Returns decoded new-order payloads.
        private static (IAcmeContext, List<Order>) CreateOrderingContext()
        {
            var dirUri = new Uri("https://example.com/directory");
            var newOrder = new Uri("https://example.com/new-order");
            var newAccount = new Uri("https://example.com/new-account");
            var orderUri = new Uri("https://example.com/acme/order/1");
            var directory = new Directory(null, newAccount, newOrder, null, null,
                new DirectoryMeta(null, null, null, null, new Dictionary<string, string> { ["shortlived"] = "Six days" }), null);
            var sent = new List<Order>();
            var http = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            http.Setup(m => m.Get<Directory>(dirUri)).ReturnsAsync(new AcmeHttpResponse<Directory>(null, directory, null, null));
            http.Setup(m => m.ConsumeNonce()).ReturnsAsync("nonce");
            http.Setup(m => m.Post<Account>(newAccount, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<Account>(new Uri("https://example.com/acct/1"), new Account(), null, null));
            http.Setup(m => m.Post<Order>(newOrder, It.IsAny<object>()))
                .Callback<Uri, object>((_, body) => sent.Add(System.Text.Json.JsonSerializer.Deserialize<Order>(
                    Jws.JwsConvert.FromBase64String(((Jws.JwsPayload)body).Payload), Json.JsonUtil.CreateSettings())))
                .ReturnsAsync(new AcmeHttpResponse<Order>(orderUri, new Order(), null, null));
            http.Setup(m => m.Post<Order>(orderUri, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<Order>(orderUri, new Order { Status = OrderStatus.Pending }, null, null));
            return (new AcmeContext(dirUri, KeyFactory.NewKey(KeyAlgorithm.ES256), http.Object), sent);
        }

        private static CliCoreSpectre CreateCli(IAcmeContext acme)
        {
            var serverUri = new Uri("https://example.com/directory");
            var settingsMock = new Mock<IUserSettings>(MockBehavior.Strict);
            settingsMock.Setup(m => m.GetDefaultServer()).ReturnsAsync(serverUri);
            settingsMock.Setup(m => m.GetAccountKey(serverUri)).ReturnsAsync(KeyFactory.NewKey(KeyAlgorithm.ES256));
            return new CliCoreSpectre(settingsMock.Object, (u, k) => acme,
                new Mock<IFileUtil>(MockBehavior.Strict).Object, new Mock<IEnvironmentVariables>(MockBehavior.Strict).Object);
        }

        // Certificate with the RFC 9773 section 4.1 example AKI keyIdentifier and serial.
        private static string Rfc9773ExampleCertificatePem()
        {
            var (_, key) = new KeyAlgorithmProvider().GetKeyPair(KeyFactory.NewKey(KeyAlgorithm.ES256).ToDer());
            var generator = new X509V3CertificateGenerator();
            generator.SetSerialNumber(new BigInteger("87654321", 16));
            generator.SetSubjectDN(new X509Name("CN=ari.example"));
            generator.SetIssuerDN(new X509Name("CN=Certes ARI Test Issuer"));
            generator.SetPublicKey(key.Public);
            generator.SetNotBefore(DateTime.UtcNow.AddDays(-1));
            generator.SetNotAfter(DateTime.UtcNow.AddDays(30));
            generator.AddExtension(X509Extensions.AuthorityKeyIdentifier, false,
                new AuthorityKeyIdentifier(Org.BouncyCastle.Utilities.Encoders.Hex.Decode("69885B6B87464041E1B37B847BA0AE2CDE01C8D4")));
            var cert = generator.Generate(new Asn1SignatureFactory("SHA256WITHECDSA", key.Private, new SecureRandom()));
            using var text = new StringWriter();
            new PemWriter(text).WriteObject(cert);
            return text.ToString();
        }
    }
}

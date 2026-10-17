using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Json;
using Moq;
using Xunit;

namespace Certes
{
    public class DnsPersistExtensionsTests
    {
        private const string Thumbprint = "NzbLsXh8uDCcd-6MNwXF4W_7noWXFZAfHkxZsRGC9Xs";
        private static readonly Uri AccountUrl = new Uri("https://ca.example/acct/123");

        private static Authorization Authorization(string domain = "example.com", bool wildcard = false) =>
            new Authorization { Identifier = new Identifier { Type = IdentifierType.Dns, Value = domain }, Wildcard = wildcard };

        private static Challenge Challenge(params string[] issuers) => new Challenge
        {
            Type = ChallengeTypes.DnsPersist01,
            IssuerDomainNames = issuers.Length == 0 ? new[] { "authority.example" } : issuers,
        };

        private static DirectoryMeta Meta(string prefix = "https://ca.example/account-hash/",
            string[] issuers = null, string[] caa = null) =>
            new DirectoryMeta(null, null, caa, null, null, issuers, prefix);

        [Theory]
        [InlineData("example.com")]
        [InlineData("EXAMPLE.COM.")]
        public void MatchesDraft02Section102Vector(string domain)
        {
            var record = DnsPersistExtensions.CreateRecord(Authorization(domain), Challenge(), Meta(), AccountUrl, Thumbprint);
            Assert.Equal("_validation-persist.example.com", record.Name);
            Assert.Equal("authority.example; accounturi=https://ca.example/account-hash/sha-256/5SQm7n6tPh2-PlLbCKGnViTXX5z19SCN4cPGQHSk-kw", record.Value);
            Assert.Equal(record.Value, Assert.Single(record.TextChunks));
            Assert.Equal("(\"" + record.Value + "\")", record.ZoneFileValue);
            Assert.DoesNotContain(AccountUrl.OriginalString, record.Value);
        }

        [Fact]
        public void HashBindsDomainKeyAndExactAccountUrl()
        {
            var baseline = DnsPersistExtensions.CreateRecord(Authorization(), Challenge(), Meta(), AccountUrl, Thumbprint).Value;
            Assert.NotEqual(baseline, DnsPersistExtensions.CreateRecord(Authorization("other.example.com"), Challenge(), Meta(), AccountUrl, Thumbprint).Value);
            Assert.NotEqual(baseline, DnsPersistExtensions.CreateRecord(Authorization(), Challenge(), Meta(), AccountUrl, new string('A', 43)).Value);
            Assert.NotEqual(baseline, DnsPersistExtensions.CreateRecord(Authorization(), Challenge(), Meta(), new Uri("https://CA.example/acct/123"), Thumbprint).Value);
        }

        [Fact]
        public void NormalizesIdnAndRequiresExplicitWildcardPolicy()
        {
            var authz = Authorization("BÜCHER.example.", true);
            Assert.Throws<ArgumentException>(() => DnsPersistExtensions.CreateRecord(authz, Challenge(), Meta(), AccountUrl, Thumbprint));
            var until = DateTimeOffset.UtcNow.AddDays(7);
            var record = DnsPersistExtensions.CreateRecord(authz, Challenge(), Meta(), AccountUrl, Thumbprint, true, until);
            Assert.Equal("_validation-persist.xn--bcher-kva.example", record.Name);
            Assert.EndsWith("; policy=wildcard; persistUntil=" + until.ToUnixTimeSeconds(), record.Value);
        }

        [Fact]
        public void SplitsLongValuesIntoOneTxtRecord()
        {
            var record = DnsPersistExtensions.CreateRecord(Authorization(), Challenge(),
                Meta("https://ca.example/" + new string('a', 600) + "/"), AccountUrl, Thumbprint);
            Assert.True(record.TextChunks.Count > 1);
            Assert.All(record.TextChunks, chunk => Assert.InRange(chunk.Length, 1, 255));
            Assert.Equal(record.Value, string.Concat(record.TextChunks));
            Assert.Contains("\" \"", record.ZoneFileValue);
        }

        [Fact]
        public void EnforcesDnsOwnerAndRdataLimits()
        {
            var domain = string.Join(".", Enumerable.Repeat(new string('a', 63), 3)) + "." + new string('b', 41);
            Assert.Equal(253, DnsPersistExtensions.CreateRecord(Authorization(domain), Challenge(), Meta(), AccountUrl, Thumbprint).Name.Length);
            Assert.Throws<ArgumentException>(() => DnsPersistExtensions.CreateRecord(Authorization(domain + "b"), Challenge(), Meta(), AccountUrl, Thumbprint));
            Assert.Throws<AcmeException>(() => new DnsPersistRecord("_validation-persist.example.com", new string('a', 65535)));
        }

        [Fact]
        public void PreservesPrefixAndAcceptsAccountUrlSemicolonsAsHashInput()
        {
            var record = DnsPersistExtensions.CreateRecord(Authorization(), Challenge(),
                Meta("https://CA.example/hash/%2f"), new Uri("https://ca.example/acct/123;id=1"), Thumbprint);
            Assert.Contains("accounturi=https://CA.example/hash/%2fsha-256/", record.Value);
            Assert.DoesNotContain(";id=1", record.Value);
        }

        [Theory]
        [InlineData("*.example.com")]
        [InlineData("example..com")]
        [InlineData("192.0.2.1")]
        [InlineData("-bad.example")]
        [InlineData("a;policy=wildcard")]
        [InlineData("")]
        public void RejectsInvalidDomains(string domain) =>
            Assert.Throws<ArgumentException>(() => DnsPersistExtensions.CreateRecord(Authorization(domain), Challenge(), Meta(), AccountUrl, Thumbprint));

        [Theory]
        [InlineData("CA.example")]
        [InlineData("ca.example.")]
        [InlineData("bücher.example")]
        [InlineData("ca.example; policy=wildcard")]
        [InlineData(null)]
        public void RejectsNonconformingIssuer(string issuer) =>
            Assert.Throws<AcmeException>(() => DnsPersistExtensions.CreateRecord(Authorization(), Challenge(new[] { issuer }), Meta(), AccountUrl, Thumbprint));

        [Fact]
        public void RejectsMissingAndExcessiveIssuerArraysAndOldDraft()
        {
            foreach (var issuers in new[] { null, Array.Empty<string>(), Enumerable.Repeat("authority.example", 11).ToArray() })
            {
                var challenge = Challenge();
                challenge.IssuerDomainNames = issuers;
                Assert.Throws<AcmeException>(() => DnsPersistExtensions.CreateRecord(Authorization(), challenge, Meta(), AccountUrl, Thumbprint));
            }

            var old = JsonSerializer.Deserialize<Challenge>("{\"type\":\"dns-persist-01\",\"issuer-domain-names\":[\"authority.example\"]}", JsonUtil.CreateSettings());
            Assert.Throws<AcmeException>(() => DnsPersistExtensions.CreateRecord(Authorization(), old, Meta(), AccountUrl, Thumbprint));
        }

        [Fact]
        public void EnforcesDirectorySubsetsAndIssuerSelection()
        {
            var challenge = Challenge("authority.example", "other.example");
            Assert.Throws<AcmeException>(() => DnsPersistExtensions.CreateRecord(Authorization(), challenge,
                Meta(issuers: new[] { "missing.example" }), AccountUrl, Thumbprint));
            Assert.Throws<AcmeException>(() => DnsPersistExtensions.CreateRecord(Authorization(), challenge,
                Meta(caa: new[] { "authority.example" }), AccountUrl, Thumbprint));
            Assert.Throws<ArgumentException>(() => DnsPersistExtensions.CreateRecord(Authorization(), challenge,
                Meta(), AccountUrl, Thumbprint, issuerDomainName: "missing.example"));
            var record = DnsPersistExtensions.CreateRecord(Authorization(), challenge,
                Meta(issuers: new[] { "authority.example" }, caa: new[] { "AUTHORITY.EXAMPLE.", "other.example" }),
                AccountUrl, Thumbprint, issuerDomainName: "other.example");
            Assert.StartsWith("other.example;", record.Value);
            // Malformed directory identities are unavailable, as specified in section 3.2.
            DnsPersistExtensions.CreateRecord(Authorization(), challenge,
                Meta(issuers: new[] { "UPPER.example" }, caa: new[] { "bad;value" }), AccountUrl, Thumbprint);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("relative/")]
        [InlineData("https://ca.example/;policy=wildcard;")]
        [InlineData("https://ca.example/\n")]
        public void RejectsMissingOrUnsafePrefix(string prefix) =>
            Assert.Throws<AcmeException>(() => DnsPersistExtensions.CreateRecord(Authorization(), Challenge(), Meta(prefix), AccountUrl, Thumbprint));

        [Fact]
        public void RejectsIpIdentifiersExpiredRecordsAndWrongChallenge()
        {
            var authz = Authorization();
            authz.Identifier.Type = IdentifierType.Ip;
            Assert.Throws<ArgumentException>(() => DnsPersistExtensions.CreateRecord(authz, Challenge(), Meta(), AccountUrl, Thumbprint));
            Assert.Throws<ArgumentOutOfRangeException>(() => DnsPersistExtensions.CreateRecord(Authorization(), Challenge(), Meta(), AccountUrl, Thumbprint,
                persistUntil: DateTimeOffset.UtcNow.AddMinutes(-1)));
            var challenge = Challenge();
            challenge.Type = ChallengeTypes.Dns01;
            Assert.Throws<AcmeException>(() => DnsPersistExtensions.CreateRecord(Authorization(), challenge, Meta(), AccountUrl, Thumbprint));
        }

        [Fact]
        public void DeserializesCurrentDraftMetadata()
        {
            var options = JsonUtil.CreateSettings();
            var challenge = JsonSerializer.Deserialize<Challenge>("{\"type\":\"dns-persist-01\",\"issuerDomainNames\":[\"authority.example\"]}", options);
            var meta = JsonSerializer.Deserialize<DirectoryMeta>("{\"issuerDomainNames\":[\"authority.example\"],\"accountHashPrefix\":\"https://ca.example/account-hash/\"}", options);
            Assert.Null(challenge.Token);
            Assert.Contains("5SQm7n6tPh2-PlLbCKGnViTXX5z19SCN4cPGQHSk-kw",
                DnsPersistExtensions.CreateRecord(Authorization(), challenge, meta, AccountUrl, Thumbprint).Value);
        }

        [Fact]
        public async Task ForwardsCancellationAndSelectsTokenlessChallenge()
        {
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            var account = new Mock<IAccountContext>(MockBehavior.Strict);
            account.SetupGet(a => a.Location).Returns(AccountUrl);
            var context = new Mock<IAcmeContext>(MockBehavior.Strict);
            context.Setup(c => c.GetDirectory(token)).ReturnsAsync(new Acme.Resource.Directory(null, null, null, null, null, Meta(), null));
            context.Setup(c => c.Account(token)).ReturnsAsync(account.Object);
            context.SetupGet(c => c.AccountKey).Returns(KeyFactory.NewKey(KeyAlgorithm.ES256));
            Assert.NotNull(await context.Object.GetDnsPersistRecord(Authorization(), Challenge(), cancellationToken: token));

            var challenge = new Mock<IChallengeContext>(MockBehavior.Strict);
            challenge.SetupGet(c => c.Type).Returns(ChallengeTypes.DnsPersist01);
            var authorization = new Mock<IAuthorizationContext>(MockBehavior.Strict);
            authorization.Setup(a => a.Challenges(token)).ReturnsAsync(new[] { challenge.Object });
            Assert.Same(challenge.Object, await authorization.Object.DnsPersist(token));
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Object.GetDnsPersistRecord(Authorization(), Challenge(), cancellationToken: token));
            context.Verify(c => c.GetDirectory(token), Times.Once);
        }
    }
}

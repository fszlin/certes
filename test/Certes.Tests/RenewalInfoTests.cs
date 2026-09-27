using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Crypto;
using Certes.Json;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.X509;
using Moq;
using Xunit;
using Directory = Certes.Acme.Resource.Directory;

namespace Certes
{
    public class RenewalInfoTests
    {
        private static readonly Uri DirectoryUri = new Uri("http://acme.d/dict");

        [Fact]
        public void CertificateIdMatchesRfc9773Example()
        {
            // RFC 9773 section 4.1: AKI keyIdentifier 69:88:5B:6B:87:46:40:41:E1:B3:7B:84:7B:A0:AE:2C:DE:01:C8:D4,
            // serial 00:87:65:43:21.
            var chain = IssueWithAki(
                Hex.Decode("69885B6B87464041E1B37B847BA0AE2CDE01C8D4"),
                new BigInteger("87654321", 16));

            Assert.Equal("aYhba4dGQEHhs3uEe6CuLN4ByNQ.AIdlQyE", chain.GetRenewalInfoCertificateId());
            Assert.Equal("aYhba4dGQEHhs3uEe6CuLN4ByNQ.AIdlQyE", chain.Certificate.GetRenewalInfoCertificateId());
        }

        [Fact]
        public void CertificateIdUsesUrlSafeAlphabetWithoutPadding()
        {
            var chain = IssueWithAki(
                Hex.Decode("FBFF" + new string('0', 36)),
                BigInteger.ValueOf(0x7F));

            var id = chain.GetRenewalInfoCertificateId();

            Assert.Equal("-_8AAAAAAAAAAAAAAAAAAAAAAAA.fw", id);
            Assert.DoesNotContain("=", id);
        }

        [Fact]
        public void CertificateIdRequiresAuthorityKeyIdentifier()
        {
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);

            Assert.Throws<AcmeException>(() => fixture.Chain.GetRenewalInfoCertificateId());
            Assert.Throws<ArgumentNullException>(() => ((CertificateChain)null).GetRenewalInfoCertificateId());
        }

        [Fact]
        public void CanDeserializeDirectoryWithRenewalInfo()
        {
            var json = "{\"newNonce\":\"http://acme.d/newNonce\",\"renewalInfo\":\"http://acme.d/renewal-info\"}";
            var dir = JsonSerializer.Deserialize<Directory>(json, JsonUtil.CreateSettings());

            Assert.Equal(new Uri("http://acme.d/newNonce"), dir.NewNonce);
            Assert.Equal(new Uri("http://acme.d/renewal-info"), dir.RenewalInfo);

            var legacy = JsonSerializer.Deserialize<Directory>("{\"newNonce\":\"http://acme.d/n\"}", JsonUtil.CreateSettings());
            Assert.Null(legacy.RenewalInfo);
            Assert.Null(new Directory(null, null, null, null, null, null).RenewalInfo);
        }

        [Fact]
        public async Task CanGetRenewalInfo()
        {
            var json = "{\"suggestedWindow\":{\"start\":\"2025-01-02T04:00:00Z\",\"end\":\"2025-01-03T04:00:00Z\"}," +
                "\"explanationURL\":\"https://acme.example.com/docs/ari\"}";
            var info = JsonSerializer.Deserialize<RenewalInfo>(json, JsonUtil.CreateSettings());
            var expectedUri = new Uri("http://acme.d/renewalInfo/aYhba4dGQEHhs3uEe6CuLN4ByNQ.AIdlQyE");

            var http = new Mock<IAcmeHttpClient>();
            http.Setup(m => m.Get<RenewalInfo>(expectedUri))
                .ReturnsAsync(new AcmeHttpResponse<RenewalInfo>(null, info, null, null, 21600));
            var ctx = CreateContext(http, Helper.MockDirectoryV2);

            var result = await ctx.Object.GetRenewalInfo("aYhba4dGQEHhs3uEe6CuLN4ByNQ.AIdlQyE");

            Assert.Equal(DateTimeOffset.Parse("2025-01-02T04:00:00Z"), result.SuggestedWindow.Start);
            Assert.Equal(DateTimeOffset.Parse("2025-01-03T04:00:00Z"), result.SuggestedWindow.End);
            Assert.Equal(new Uri("https://acme.example.com/docs/ari"), result.ExplanationUrl);
            Assert.Equal(TimeSpan.FromHours(6), result.RetryAfter);
            http.Verify(m => m.Get<RenewalInfo>(expectedUri), Times.Once);
            http.Verify(m => m.Post<It.IsAnyType>(It.IsAny<Uri>(), It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task GetRenewalInfoPreservesEndpointPath()
        {
            var dir = new Directory(null, null, null, null, null, null, new Uri("http://acme.d/acme/renewal-info/"));
            var expectedUri = new Uri("http://acme.d/acme/renewal-info/abc.AQ");
            var http = new Mock<IAcmeHttpClient>();
            http.Setup(m => m.Get<RenewalInfo>(expectedUri))
                .ReturnsAsync(new AcmeHttpResponse<RenewalInfo>(null, new RenewalInfo(), null, null));
            var ctx = CreateContext(http, dir);

            var result = await ctx.Object.GetRenewalInfo("abc.AQ");

            Assert.Null(result.RetryAfter);
            http.Verify(m => m.Get<RenewalInfo>(expectedUri), Times.Once);
        }

        [Fact]
        public async Task GetRenewalInfoFailures()
        {
            var http = new Mock<IAcmeHttpClient>();
            var error = new AcmeError { Status = HttpStatusCode.NotFound, Type = "urn:ietf:params:acme:error:malformed" };
            http.Setup(m => m.Get<RenewalInfo>(It.IsAny<Uri>()))
                .ReturnsAsync(new AcmeHttpResponse<RenewalInfo>(null, null, null, error));

            var ctx = CreateContext(http, Helper.MockDirectoryV2);
            var ex = await Assert.ThrowsAsync<AcmeRequestException>(() => ctx.Object.GetRenewalInfo("abc.AQ"));
            Assert.Equal(error.Type, ex.Error.Type);

            await Assert.ThrowsAsync<ArgumentException>(() => ctx.Object.GetRenewalInfo(""));
            await Assert.ThrowsAsync<ArgumentException>(() => ctx.Object.GetRenewalInfo("../x"));

            var unsupported = CreateContext(http, new Directory(null, null, null, null, null, null));
            await Assert.ThrowsAsync<NotSupportedException>(() => unsupported.Object.GetRenewalInfo("abc.AQ"));
        }

        [Fact]
        public async Task CanCreateReplacementOrder()
        {
            var orderLoc = new Uri("http://acme.d/order/1");
            var http = new Mock<IAcmeHttpClient>();
            http.Setup(m => m.Post<Order>(Helper.MockDirectoryV2.NewOrder, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<Order>(orderLoc, new Order(), null, null));
            var ctx = CreateContext(http, Helper.MockDirectoryV2);
            object signed = null;
            ctx.Setup(m => m.Sign(It.IsAny<object>(), Helper.MockDirectoryV2.NewOrder))
                .Callback<object, Uri>((e, _) => signed = e)
                .ReturnsAsync(new Jws.JwsPayload());

            var order = await ctx.Object.NewReplacementOrder(new[] { "www.certes.com" }, "abc.AQ");

            Assert.Equal(orderLoc, order.Location);
            var body = Assert.IsType<Order>(signed);
            Assert.Equal("abc.AQ", body.Replaces);
            Assert.Equal("www.certes.com", body.Identifiers.Single().Value);

            var wire = JsonSerializer.Serialize(body, JsonUtil.CreateSettings());
            Assert.Contains("\"replaces\":\"abc.AQ\"", wire);
            Assert.DoesNotContain("replaces", JsonSerializer.Serialize(new Order(), JsonUtil.CreateSettings()));

            await Assert.ThrowsAsync<ArgumentException>(
                () => ctx.Object.NewReplacementOrder(new[] { "www.certes.com" }, null));
        }

        private static Mock<IAcmeContext> CreateContext(Mock<IAcmeHttpClient> http, Directory dir)
        {
            var ctx = new Mock<IAcmeContext>();
            ctx.SetupGet(m => m.HttpClient).Returns(http.Object);
            ctx.Setup(m => m.GetDirectory()).ReturnsAsync(dir);
            return ctx;
        }

        private static CertificateChain IssueWithAki(byte[] keyIdentifier, BigInteger serial)
        {
            var (_, key) = new KeyAlgorithmProvider().GetKeyPair(KeyFactory.NewKey(KeyAlgorithm.ES256).ToDer());
            var generator = new X509V3CertificateGenerator();
            generator.SetSerialNumber(serial);
            generator.SetSubjectDN(new X509Name("CN=ari.example"));
            generator.SetIssuerDN(new X509Name("CN=Certes ARI Test Issuer"));
            generator.SetPublicKey(key.Public);
            generator.SetNotBefore(DateTime.UtcNow.AddDays(-1));
            generator.SetNotAfter(DateTime.UtcNow.AddDays(30));
            generator.AddExtension(X509Extensions.AuthorityKeyIdentifier, false, new AuthorityKeyIdentifier(keyIdentifier));
            var cert = generator.Generate(new Asn1SignatureFactory("SHA256WITHECDSA", key.Private, new SecureRandom()));

            using (var text = new System.IO.StringWriter())
            {
                new PemWriter(text).WriteObject(cert);
                return new CertificateChain(text.ToString());
            }
        }
    }
}

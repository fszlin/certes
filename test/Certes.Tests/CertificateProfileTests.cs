using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Json;
using Certes.Jws;
using Moq;
using Xunit;
using Directory = Certes.Acme.Resource.Directory;

namespace Certes
{
    public class CertificateProfileTests
    {
        [Fact]
        public void DirectoryProfilesRoundTripWithDescriptionsAndLegacyConstructor()
        {
            const string json = "{\"meta\":{\"profiles\":{\"classic\":\"A prose description\",\"tlsserver\":\"https://ca.example/profiles/tlsserver\"}}}";
            var directory = JsonSerializer.Deserialize<Directory>(json, JsonUtil.CreateSettings());
            Assert.Equal("A prose description", directory.Meta.Profiles["classic"]);
            Assert.Equal("https://ca.example/profiles/tlsserver", directory.Meta.Profiles["tlsserver"]);
            var roundTrip = JsonSerializer.Deserialize<Directory>(
                JsonSerializer.Serialize(directory, JsonUtil.CreateSettings()), JsonUtil.CreateSettings());
            Assert.Equal(directory.Meta.Profiles, roundTrip.Meta.Profiles);
            Assert.Null(new DirectoryMeta(null, null, null, null).Profiles);
            Assert.Null(JsonSerializer.Deserialize<DirectoryMeta>("{}", JsonUtil.CreateSettings()).Profiles);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("abc.AQ")]
        public async Task SelectedProfileAndReplacementAreIncludedInSignedOrder(string replacementId)
        {
            var http = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            var directory = CreateDirectory(new Dictionary<string, string> { ["tlsserver"] = "TLS server" });
            var ctx = CreateContext(http, directory);
            var location = new Uri("http://acme.d/order/1");
            JwsPayload payload = null;
            http.Setup(m => m.Post<Order>(directory.NewOrder, It.IsAny<object>()))
                .Callback<Uri, object>((_, body) => payload = Assert.IsType<JwsPayload>(body))
                .ReturnsAsync(new AcmeHttpResponse<Order>(location, new Order { Profile = "tlsserver" }, null, null));
            var start = new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero);
            var end = start.AddDays(6);

            var order = await ctx.NewOrderWithProfile(new[] { "profile.example" }, "tlsserver", start, end, replacementId);

            Assert.Equal(location, order.Location);
            using var wire = JsonDocument.Parse(Encoding.UTF8.GetString(JwsConvert.FromBase64String(payload.Payload)));
            Assert.Equal("tlsserver", wire.RootElement.GetProperty("profile").GetString());
            Assert.Equal("dns", wire.RootElement.GetProperty("identifiers")[0].GetProperty("type").GetString());
            Assert.Equal("profile.example", wire.RootElement.GetProperty("identifiers")[0].GetProperty("value").GetString());
            Assert.Equal(start, wire.RootElement.GetProperty("notBefore").GetDateTimeOffset());
            Assert.Equal(end, wire.RootElement.GetProperty("notAfter").GetDateTimeOffset());
            if (replacementId == null)
            {
                Assert.False(wire.RootElement.TryGetProperty("replaces", out _));
            }
            else
            {
                Assert.Equal(replacementId, wire.RootElement.GetProperty("replaces").GetString());
            }

            Assert.Equal("tlsserver", JsonSerializer.Deserialize<Order>("{\"profile\":\"tlsserver\"}", JsonUtil.CreateSettings()).Profile);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExistingOrderApisOmitProfile(bool replacement)
        {
            var http = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            var directory = CreateDirectory(null);
            var ctx = CreateContext(http, directory);
            http.Setup(m => m.Post<Order>(directory.NewOrder, It.IsAny<object>()))
                .Callback<Uri, object>((uri, body) =>
                {
                    var payload = Assert.IsType<JwsPayload>(body);
                    using var wire = JsonDocument.Parse(JwsConvert.FromBase64String(payload.Payload));
                    Assert.False(wire.RootElement.TryGetProperty("profile", out _));
                })
                .ReturnsAsync(new AcmeHttpResponse<Order>(new Uri("http://acme.d/order/1"), new Order(), null, null));

            if (replacement)
            {
                await ctx.NewReplacementOrder(new[] { "profile.example" }, "abc.AQ");
            }
            else
            {
                // Existing positional-null calls must remain unambiguous.
                await ctx.NewOrder(new[] { "profile.example" }, null);
            }
            http.Verify(m => m.Post<Order>(directory.NewOrder, It.IsAny<object>()), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task EmptyProfileIsRejectedBeforeDirectoryLookup(string profile)
        {
            var ctx = new Mock<IAcmeContext>(MockBehavior.Strict);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => ctx.Object.NewOrderWithProfile(new[] { "profile.example" }, profile));
            Assert.Equal("profile", ex.ParamName);
            ctx.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("tlsserver")]
        [InlineData("Shortlived")]
        public async Task UnadvertisedProfileIsRejectedBeforePosting(string profile)
        {
            var ctx = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctx.Setup(m => m.GetDirectory()).ReturnsAsync(CreateDirectory(new Dictionary<string, string> { ["shortlived"] = "Six days" }));
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => ctx.Object.NewOrderWithProfile(new[] { "profile.example" }, profile));
            Assert.Equal("profile", ex.ParamName);
            ctx.Verify(m => m.GetDirectory(), Times.Once);
            ctx.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("{\"meta\":{}}")]
        [InlineData("{\"meta\":{\"profiles\":{}}}")]
        public async Task MissingProfilesAreUnsupported(string json)
        {
            var ctx = new Mock<IAcmeContext>(MockBehavior.Strict);
            ctx.Setup(m => m.GetDirectory()).ReturnsAsync(JsonSerializer.Deserialize<Directory>(json, JsonUtil.CreateSettings()));
            await Assert.ThrowsAsync<NotSupportedException>(() => ctx.Object.NewOrderWithProfile(new[] { "profile.example" }, "shortlived"));
        }

        [Fact]
        public async Task InvalidReplacementIdIsRejectedBeforeDirectoryLookup()
        {
            var ctx = new Mock<IAcmeContext>(MockBehavior.Strict);
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => ctx.Object.NewOrderWithProfile(
                new[] { "profile.example" }, "shortlived", replacedCertificateId: "abc/AQ"));
            Assert.Equal("replacedCertificateId", ex.ParamName);
            ctx.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ServerInvalidProfileProblemIsPreserved()
        {
            var http = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            var directory = CreateDirectory(new Dictionary<string, string> { ["shortlived"] = "Six days" });
            var ctx = CreateContext(http, directory);
            var error = new AcmeError
            {
                Type = "urn:ietf:params:acme:error:invalidProfile",
                Status = HttpStatusCode.BadRequest,
                Detail = "Profile not available for this account",
            };
            http.Setup(m => m.Post<Order>(directory.NewOrder, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<Order>(null, null, null, error));
            var ex = await Assert.ThrowsAsync<AcmeRequestException>(() => ctx.NewOrderWithProfile(new[] { "profile.example" }, "shortlived"));
            Assert.Same(error, ex.Error);
        }

        private static Directory CreateDirectory(IDictionary<string, string> profiles)
            => new Directory(null, Helper.MockDirectoryV2.NewAccount, Helper.MockDirectoryV2.NewOrder, null, null,
                new DirectoryMeta(null, null, null, null, profiles), Helper.MockDirectoryV2.RenewalInfo);

        private static AcmeContext CreateContext(Mock<IAcmeHttpClient> http, Directory directory)
        {
            var uri = new Uri("http://acme.d/directory");
            http.Setup(m => m.Get<Directory>(uri)).ReturnsAsync(new AcmeHttpResponse<Directory>(null, directory, null, null));
            http.Setup(m => m.ConsumeNonce()).ReturnsAsync("nonce");
            http.Setup(m => m.Post<Account>(directory.NewAccount, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<Account>(new Uri("http://acme.d/account/1"), new Account(), null, null));
            return new AcmeContext(uri, Helper.GetKeyV2(), http.Object);
        }
    }
}

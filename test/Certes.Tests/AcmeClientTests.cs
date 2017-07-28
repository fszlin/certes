using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Json;
using Certes.Jws;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Certes.Tests
{
    public class AcmeClientTests
    {
        private const string JsonContentType = "application/json";
        private const string email = "test@example.com";
        private const string NonceFormat = "nonce-{0}";
        private readonly Uri server = new Uri("http://example.com/dir");
        private readonly Uri tos = new Uri("http://example.com/tos");

        private readonly Directory acmeDir = Helper.AcmeDir;

        private int nonce = 0;
        private AccountKey accountKey = Helper.Loadkey();

        [Fact]
        public async Task CanCreateRegistration()
        {
            var regLocation = new Uri("http://example.com/reg/1");
            var mock = MockHttp(async req =>
            {
                if (req.Method == HttpMethod.Post && req.RequestUri == acmeDir.NewReg)
                {
                    var payload = await ParsePayload<Registration>(req);
                    Assert.Equal(ResourceTypes.NewRegistration, payload.Resource);
                    Assert.Equal(1, payload.Contact?.Count);
                    Assert.Equal(email, payload.Contact[0]);

                    var respJson = new
                    {
                        contact = payload.Contact,
                        resource = ResourceTypes.Registration
                    };

                    var resp = CreateResponse(respJson, HttpStatusCode.Created, regLocation);
                    resp.Headers.Add("Link", $"<{tos}>; rel=\"terms-of-service\"");
                    return resp;
                }

                return null;
            });

            using (var http = new HttpClient(mock.Object))
            using (var handler = new AcmeHttpHandler(server, http))
            {
                using (var client = new AcmeClient(handler))
                {
                    client.Use(accountKey.Export());

                    var account = await client.NewRegistraton(email);

                    Assert.Equal(ResourceTypes.Registration, account.Data.Resource);
                    Assert.Null(account.Data.Agreement);
                    Assert.Equal(regLocation, account.Location);
                    Assert.Equal(tos, account.GetTermsOfServiceUri());
                }

                mock.Protected().Verify("Dispose", Times.Never(), true);
            }
        }

        [Fact]
        public async Task CanUpdateRegistration()
        {
            var regLocation = new Uri("http://example.com/reg/1");
            var mock = MockHttp(async req =>
            {
                if (req.Method == HttpMethod.Post && req.RequestUri == regLocation)
                {
                    var payload = await ParsePayload<Registration>(req);
                    Assert.Equal(ResourceTypes.Registration, payload.Resource);
                    Assert.Equal(1, payload.Contact?.Count);
                    Assert.Equal($"another-{email}", payload.Contact[0]);
                    Assert.NotNull(payload.Agreement);

                    var respJson = new
                    {
                        contact = payload.Contact,
                        agreement = payload.Agreement,
                        resource = ResourceTypes.Registration
                    };

                    var resp = CreateResponse(respJson, HttpStatusCode.Created, regLocation);
                    resp.Headers.Add("Link", $"<{tos}>; rel=\"terms-of-service\"");
                    return resp;
                }

                return null;
            });

            using (var http = new HttpClient(mock.Object))
            using (var handler = new AcmeHttpHandler(server, http))
            {
                using (var client = new AcmeClient(handler))
                {
                    client.Use(accountKey.Export());

                    var account = new AcmeAccount
                    {
                        Location = regLocation,
                        Data = new Registration
                        {
                            Resource = ResourceTypes.Registration,
                            Contact = new[] { $"another-{email}" },
                            Agreement = tos
                        }
                    };

                    var result = await client.UpdateRegistration(account);
                    Assert.Equal(ResourceTypes.Registration, result.Data.Resource);
                    Assert.Equal(tos, account.Data.Agreement);
                    Assert.Equal(regLocation, account.Location);
                }

                mock.Protected().Verify("Dispose", Times.Never(), true);
            }
        }

        private async Task<T> ParsePayload<T>(HttpRequestMessage message)
        {
            var content = message.Content;
            Assert.Equal(JsonContentType, content.Headers.ContentType.MediaType);

            var json = await content.ReadAsStringAsync();
            dynamic body = JObject.Parse(json);

            Assert.Equal("RS256", body.header?.alg?.Value);
            Assert.Equal("AQAB", body.header?.jwk?.e?.Value);
            Assert.Equal("RSA", body.header?.jwk?.kty?.Value);
            Assert.Equal("maeT6EsXTVHAdwuq3IlAl9uljXE5CnkRpr6uSw_Fk9nQshfZqKFdeZHkSBvIaLirE2ZidMEYy-rpS1O2j-viTG5U6bUSWo8aoeKoXwYfwbXNboEA-P4HgGCjD22XaXAkBHdhgyZ0UBX2z-jCx1smd7nucsi4h4RhC_2cEB1x_mE6XS5VlpvG91Hbcgml4cl0NZrWPtJ4DhFdPNUtQ8q3AYXkOr_OSFZgRKjesRaqfnSdJNABqlO_jEzAx0fgJfPZe1WlRWOfGRVBVopZ4_N5HpR_9lsNDzCZyidFsHwzvpkP6R6HbS8CMrNWgtkTbnz27EVqIhkYdiPVIN2Xkwj0BQ", body.header?.jwk?.n?.Value);

            var protectedBase64 = body.GetValue("protected").Value;
            var protectedJson = Encoding.UTF8.GetString(JwsConvert.FromBase64String(protectedBase64));
            dynamic @protected = JObject.Parse(protectedJson);
            Assert.Equal(string.Format(NonceFormat, this.nonce), @protected.nonce?.Value);

            var payloadBase64 = body.GetValue("payload").Value;
            var payloadJson = Encoding.UTF8.GetString(JwsConvert.FromBase64String(payloadBase64));

            var signature = $"{protectedBase64}.{payloadBase64}";
            var signatureBytes = Encoding.ASCII.GetBytes(signature);
            var signedSignatureBytes = accountKey.SignData(signatureBytes);
            var signedSignatureEncoded = JwsConvert.ToBase64String(signedSignatureBytes);

            Assert.Equal(signedSignatureEncoded, body.GetValue("signature").Value);

            return JsonConvert.DeserializeObject<T>(payloadJson, JsonUtil.CreateSettings());
        }

        private Mock<HttpMessageHandler> MockHttp(Func<HttpRequestMessage, Task<HttpResponseMessage>> provider)
        {
            var mock = new Mock<HttpMessageHandler>();

            mock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(async (HttpRequestMessage req, CancellationToken token) =>
                {
                    if (req.Method == HttpMethod.Get && req.RequestUri == server)
                    {
                        var resp = new HttpResponseMessage();
                        resp.StatusCode = HttpStatusCode.OK;
                        resp.Content = new StringContent(JsonConvert.SerializeObject(acmeDir), Encoding.UTF8, "application/json");

                        resp.Headers.Add("Replay-Nonce", string.Format(NonceFormat, ++this.nonce));
                        return resp;
                    }
                    else
                    {
                        var resp = await provider(req);
                        Assert.NotNull(resp);
                        resp.Headers.Add("Replay-Nonce", string.Format(NonceFormat, ++this.nonce));
                        return resp;
                    }
                });

            return mock;
        }

        private HttpResponseMessage CreateResponse(object payload, HttpStatusCode statusCode, Uri location)
        {
            var resp = new HttpResponseMessage();
            resp.Headers.Location = location;
            resp.Content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, JsonContentType);
            return resp;
        }
    }
}

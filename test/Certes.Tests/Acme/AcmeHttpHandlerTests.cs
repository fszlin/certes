using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Xunit;

namespace Certes.Acme
{
    public class AcmeHttpHandlerTests
    {
        private const string NonceFormat = "nonce-{0}";
        private readonly Uri server = new Uri("http://example.com/dir");

        private int nonce = 0;

        [Fact]
        public async Task CanRetrieveResourceUriFromDirectory()
        {
            using (var http = new HttpClient(CreateHttpMock().Object))
            using (var handler = new AcmeHttpHandler(server, http))
            {
                Assert.Equal(Helper.MockDirectoryV1.NewReg, await handler.GetResourceUri(ResourceTypes.NewRegistration));
                Assert.Equal(Helper.MockDirectoryV1.NewAuthz, await handler.GetResourceUri(ResourceTypes.NewAuthorization));
                Assert.Equal(Helper.MockDirectoryV1.NewCert, await handler.GetResourceUri(ResourceTypes.NewCertificate));
                Assert.Equal(Helper.MockDirectoryV1.RevokeCert, await handler.GetResourceUri(ResourceTypes.RevokeCertificate));
            }
        }

        [Fact]
        public async Task ShouldFaildWithInvalidResourceType()
        {
            using (var http = new HttpClient(CreateHttpMock().Object))
            using (var handler = new AcmeHttpHandler(server, http))
            {
                await Assert.ThrowsAsync<AcmeException>(async () => await handler.GetResourceUri("invalid-type"));
            }
        }

        [Fact]
        public void CanCreateInstance()
        {
            using (var handler = new AcmeHttpHandler(server)) { }

            using (var http = new HttpClient())
            {
                using (var handler = new AcmeHttpHandler(server, http)) { }
            }

            using (var handler = new AcmeHttpHandler(server, (HttpClient)null)) { }

#pragma warning disable 0618
            using (var handler = new AcmeHttpHandler(server, (HttpMessageHandler)null)) { }
            using (var handler = new AcmeHttpHandler(server, CreateHttpMock().Object)) { }
#pragma warning restore 0618
        }

        [Fact]
        public void CanGetServerUri()
        {
            using (var handler = new AcmeHttpHandler(server, (HttpClient)null))
            {
                Assert.Equal(server, handler.ServerUri);
            }
        }

        private Mock<HttpMessageHandler> CreateHttpMock()
        {
            var mock = new Mock<HttpMessageHandler>();

            mock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage req, CancellationToken token) =>
                {
                    var resp = new HttpResponseMessage();
                    resp.StatusCode = HttpStatusCode.OK;
                    resp.Content = new StringContent(JsonConvert.SerializeObject(Helper.MockDirectoryV1), Encoding.UTF8, "application/json");

                    resp.Headers.Add("Replay-Nonce", string.Format(NonceFormat, ++this.nonce));
                    return Task.FromResult(resp);
                });

            return mock;
        }
    }
}

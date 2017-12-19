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
                Assert.Equal(Helper.AcmeDir.NewReg, await handler.GetResourceUri(ResourceTypes.NewRegistration));
                Assert.Equal(Helper.AcmeDir.NewAuthz, await handler.GetResourceUri(ResourceTypes.NewAuthorization));
                Assert.Equal(Helper.AcmeDir.NewCert, await handler.GetResourceUri(ResourceTypes.NewCertificate));
                Assert.Equal(Helper.AcmeDir.RevokeCert, await handler.GetResourceUri(ResourceTypes.RevokeCertificate));
            }
        }

        [Fact]
        public async Task ShouldFaildWithInvalidResourceType()
        {
            using (var http = new HttpClient(CreateHttpMock().Object))
            using (var handler = new AcmeHttpHandler(server, http))
            {
                await Assert.ThrowsAsync<Exception>(async () => await handler.GetResourceUri("invalid-type"));
            }
        }

        [Fact]
        public async Task CanCreateInstance()
        {
            using (var handler = new AcmeHttpHandler(await Helper.GetStagingServer())) { }

            using (var http = new HttpClient())
            {
                using (var handler = new AcmeHttpHandler(await Helper.GetStagingServer(), http)) { }
            }

            using (var handler = new AcmeHttpHandler(await Helper.GetStagingServer(), (HttpClient)null)) { }

#pragma warning disable 0618
            using (var handler = new AcmeHttpHandler(await Helper.GetStagingServer(), (HttpMessageHandler)null)) { }
            using (var handler = new AcmeHttpHandler(await Helper.GetStagingServer(), CreateHttpMock().Object)) { }
#pragma warning restore 0618
        }

        [Fact]
        public async Task CanGetServerUri()
        {
            using (var handler = new AcmeHttpHandler(await Helper.GetStagingServer(), (HttpClient)null))
            {
                Assert.Equal(await Helper.GetStagingServer(), handler.ServerUri);
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
                    resp.Content = new StringContent(JsonConvert.SerializeObject(Helper.AcmeDir), Encoding.UTF8, "application/json");

                    resp.Headers.Add("Replay-Nonce", string.Format(NonceFormat, ++this.nonce));
                    return Task.FromResult(resp);
                });

            return mock;
        }
    }
}

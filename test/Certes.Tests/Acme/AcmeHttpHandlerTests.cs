using Certes.Acme;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Certes.Tests.Acme
{
    public class AcmeHttpHandlerTests
    {
        private const string NonceFormat = "nonce-{0}";
        private readonly Uri server = new Uri("http://example.com/dir");

        private int nonce = 0;

        [Fact]
        public async Task CanRetrieveResourceUriFromDirectory()
        {
            using (var handler = new AcmeHttpHandler(server, CreateHttpMock().Object))
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
            using (var handler = new AcmeHttpHandler(server, CreateHttpMock().Object))
            {
                await Assert.ThrowsAsync<Exception>(async () => await handler.GetResourceUri("invalid-type"));
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

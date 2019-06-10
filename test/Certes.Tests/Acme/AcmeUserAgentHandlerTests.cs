using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

namespace Certes.Acme
{
    public class AcmeUserAgentHandlerTests
    {
        [Fact]
        public async Task UserAgentSetByHandler()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns((HttpRequestMessage request, CancellationToken __) =>
                {                    
                    Assert.True(request.Headers.UserAgent.Count > 1);
                    Assert.Equal("Certes",request.Headers.UserAgent.FirstOrDefault()?.Product.Name);
                    Assert.Equal(typeof(AcmeUserAgentHandler).GetTypeInfo().Assembly.GetName().Version.ToString(), request.Headers.UserAgent.FirstOrDefault()?.Product.Version);
                    Assert.Equal("(ACME 2.0)", request.Headers.UserAgent.Skip(1).FirstOrDefault()?.Comment);

                    var resp = new HttpResponseMessage();
                    resp.StatusCode = HttpStatusCode.OK;                    
                    return Task.FromResult(resp);
                }).Verifiable();

            var client = HttpClientFactory.CreateInternalHttpClient(handlerMock.Object);
            await client.GetAsync("http://acme.d/directory");            
        }
    }    
}

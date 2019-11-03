using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Certes.Json;
using Newtonsoft.Json;
using Xunit;

namespace Certes.Acme
{
    public class AcmeHttpClientTests
    {
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly JsonSerializerSettings jsonSettings = JsonUtil.CreateSettings();

            public bool SendNonce { get;set; } = true;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Yield();

                if (request.RequestUri.AbsoluteUri.EndsWith("directory"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonConvert.SerializeObject(Helper.MockDirectoryV2, jsonSettings), Encoding.UTF8, "application/json"),
                    };
                }
                else if (request.RequestUri.AbsoluteUri.EndsWith("newNonce"))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    if (SendNonce)
                    {
                        resp.Headers.Add("Replay-Nonce", "nonce");
                    }

                    return resp;
                }

                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }
        }

        [Fact]
        public async Task ThrowWhenNoNonce()
        {
            var dirUri = new Uri("https://acme.d/directory");

            var httpHandler = new MockHttpMessageHandler
            {
                SendNonce = false
            };

            using (var http = new HttpClient(httpHandler))
            {
                var client = new AcmeHttpClient(dirUri, http);
                await Assert.ThrowsAsync<AcmeException>(() => client.ConsumeNonce());
            }
        }

    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Json;
using Moq;
using Newtonsoft.Json;
using Xunit;

using static Certes.Helper;

namespace Certes.Acme
{
    public class AcmeHttpClientTests
    {
        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly string productVersion = 
                typeof(AcmeHttpClient).GetTypeInfo().Assembly.GetName().Version.ToString();
            private readonly JsonSerializerSettings jsonSettings = JsonUtil.CreateSettings();

            public bool SendNonce { get; set; } = true;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await Task.Yield();

                var isCertes = false;
                foreach (var header in request.Headers.UserAgent)
                {
                    if (header.Product.Name == "Certes" &&
                        header.Product.Version == productVersion) {
                        isCertes = true;
                    }
                }

                Assert.True(isCertes, "No user-agent header");

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
        
        [Fact]
        public async Task RetryOnBadNonce()
        {
            var accountLoc = new Uri("https://acme.d/acct/1");
            var httpMock = new Mock<IAcmeHttpClient>();
            httpMock.Setup(m => m.Get<Directory>(It.IsAny<Uri>()))
                .ReturnsAsync(new AcmeHttpResponse<Directory>(
                    accountLoc, MockDirectoryV2, null, null));
            httpMock.SetupSequence(
                m => m.Post<Account>(MockDirectoryV2.NewAccount, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<Account>(
                    accountLoc, null, null, new AcmeError
                    {
                        Status = HttpStatusCode.BadRequest,
                        Type = "urn:ietf:params:acme:error:badNonce"
                    }))
                .ReturnsAsync(new AcmeHttpResponse<Account>(
                    accountLoc, new Account
                    {
                        Status = AccountStatus.Valid
                    }, null, null));
            
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var ctx = new AcmeContext(
                WellKnownServers.LetsEncryptStagingV2,
                key,
                httpMock.Object);

            await ctx.NewAccount("", true);
            httpMock.Verify(m => m.Post<Account>(MockDirectoryV2.NewAccount, It.IsAny<object>()), Times.Exactly(2));

        }

        [Fact]
        public async Task ThrowOnMultipleBadNonce()
        {
            var accountLoc = new Uri("https://acme.d/acct/1");
            var httpMock = new Mock<IAcmeHttpClient>();
            httpMock.Setup(m => m.Get<Directory>(It.IsAny<Uri>()))
                .ReturnsAsync(new AcmeHttpResponse<Directory>(
                    accountLoc, MockDirectoryV2, null, null));
            httpMock.SetupSequence(
                m => m.Post<Account>(MockDirectoryV2.NewAccount, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<Account>(
                    accountLoc, null, null, new AcmeError
                    {
                        Status = HttpStatusCode.BadRequest,
                        Type = "urn:ietf:params:acme:error:badNonce"
                    }))
                .ReturnsAsync(new AcmeHttpResponse<Account>(
                    accountLoc, null, null, new AcmeError
                    {
                        Status = HttpStatusCode.BadRequest,
                        Type = "urn:ietf:params:acme:error:badNonce"
                    }))
                .ReturnsAsync(new AcmeHttpResponse<Account>(
                    accountLoc, new Account
                    {
                        Status = AccountStatus.Valid
                    }, null, null));

            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var ctx = new AcmeContext(
                WellKnownServers.LetsEncryptStagingV2,
                key,
                httpMock.Object);

            await Assert.ThrowsAsync<AcmeRequestException>(() => ctx.NewAccount("", true));
            httpMock.Verify(m => m.Post<Account>(MockDirectoryV2.NewAccount, It.IsAny<object>()), Times.AtMost(2));
        }
    }
}

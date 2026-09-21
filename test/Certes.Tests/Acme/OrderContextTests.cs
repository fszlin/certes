using System;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;
using Moq;
using Xunit;

namespace Certes.Acme
{
    public class OrderContextTests
    {
        private Uri location = new Uri("http://acme.d/order/101");
        private Mock<IAcmeContext> contextMock = new Mock<IAcmeContext>(MockBehavior.Strict);
        private Mock<IAcmeHttpClient> httpClientMock = new Mock<IAcmeHttpClient>(MockBehavior.Strict);

        [Fact]
        public async Task CanLoadAuthorizations()
        {
            var order = new Order
            {
                Authorizations = new[]
                {
                    new Uri("http://acme.d/acct/1/authz/1"),
                    new Uri("http://acme.d/acct/1/authz/2"),
                }
            };

            var expectedPayload = new JwsSigner(Helper.GetKeyV2())
                .Sign("", null, location, "nonce");

            contextMock.Reset();
            httpClientMock.Reset();

            contextMock
                .Setup(c => c.GetDirectory())
                .ReturnsAsync(Helper.MockDirectoryV2);
            contextMock
                .SetupGet(c => c.AccountKey)
                .Returns(Helper.GetKeyV2());
            contextMock
                .SetupGet(c => c.BadNonceRetryCount)
                .Returns(1);
            contextMock.SetupGet(c => c.HttpClient).Returns(httpClientMock.Object);
            contextMock
                .Setup(c => c.Sign(It.IsAny<object>(), It.IsAny<Uri>()))
                .Callback((object payload, Uri loc) =>
                {
                    Assert.Null(payload);
                    Assert.Equal(location, loc);
                })
                .ReturnsAsync(expectedPayload);
            httpClientMock
                .Setup(m => m.Post<Order>(location, It.IsAny<JwsPayload>()))
                .Callback((Uri _, object o) =>
                {
                    var p = (JwsPayload)o;
                    Assert.Equal(expectedPayload.Payload, p.Payload);
                    Assert.Equal(expectedPayload.Protected, p.Protected);
                })
                .ReturnsAsync(new AcmeHttpResponse<Order>(location, order, default, default));

            var ctx = new OrderContext(contextMock.Object, location);
            var authzs = await ctx.Authorizations();
            Assert.Equal(order.Authorizations, authzs.Select(a => a.Location));

            // check the context returns empty list instead of null
            httpClientMock
                .Setup(m => m.Post<Order>(location, It.IsAny<JwsPayload>()))
                .ReturnsAsync(new AcmeHttpResponse<Order>(location, new Order(), default, default));
            authzs = await ctx.Authorizations();
            Assert.Empty(authzs);

        }

        [Fact]
        public async Task FinalizePropagatesRetryAfter()
        {
            var order = new Order
            {
                Finalize = new Uri("http://acme.d/order/101/finalize"),
            };
            var finalized = new Order { Status = OrderStatus.Processing };

            contextMock.SetupGet(c => c.HttpClient).Returns(httpClientMock.Object);
            contextMock.SetupGet(c => c.BadNonceRetryCount).Returns(1);
            contextMock
                .Setup(c => c.Sign(It.IsAny<object>(), It.IsAny<Uri>()))
                .ReturnsAsync(new JwsPayload());
            httpClientMock
                .SetupSequence(m => m.Post<Order>(It.IsAny<Uri>(), It.IsAny<JwsPayload>()))
                .ReturnsAsync(new AcmeHttpResponse<Order>(location, order, default, default, 3))
                .ReturnsAsync(new AcmeHttpResponse<Order>(location, finalized, default, default, 11));

            var ctx = new OrderContext(contextMock.Object, location);
            var result = await ctx.Finalize(new byte[] { 1, 2, 3 });

            Assert.Equal(finalized, result);
            Assert.Equal(11, ctx.RetryAfter);
        }

        [Fact]
        public async Task DownloadFallsBackToDefaultChainWhenAlternateLinksAreMissing()
        {
            var order = new Order
            {
                Certificate = new Uri("http://acme.d/order/101/cert/1"),
            };
            var certificate = System.IO.File.ReadAllText("./Data/defaultLeaf.pem");

            contextMock.SetupGet(c => c.HttpClient).Returns(httpClientMock.Object);
            contextMock.SetupGet(c => c.BadNonceRetryCount).Returns(1);
            contextMock
                .Setup(c => c.Sign(It.IsAny<object>(), It.IsAny<Uri>()))
                .ReturnsAsync(new JwsPayload());
            httpClientMock
                .Setup(m => m.Post<Order>(location, It.IsAny<JwsPayload>()))
                .ReturnsAsync(new AcmeHttpResponse<Order>(location, order, default, default));
            httpClientMock
                .Setup(m => m.Post<string>(order.Certificate, It.IsAny<JwsPayload>()))
                .ReturnsAsync(new AcmeHttpResponse<string>(order.Certificate, certificate, null, null));

            var ctx = new OrderContext(contextMock.Object, location);
            var result = await ctx.Download("UnknownRoot");

            Assert.Equal(
                certificate.Replace("\r", string.Empty).Replace("\n", string.Empty),
                result.Certificate.ToPem().Replace("\r", string.Empty).Replace("\n", string.Empty));
            httpClientMock.Verify(m => m.Post<string>(order.Certificate, It.IsAny<JwsPayload>()), Times.Once);
        }
    }
}

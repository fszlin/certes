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
    }
}

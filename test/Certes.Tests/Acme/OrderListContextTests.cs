using System;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Moq;
using Xunit;

namespace Certes.Acme
{
    public class OrderListContextTests
    {
        [Fact]
        public async Task StopsWhenNextLinkIsMissing()
        {
            var location = new Uri("http://acme.d/acct/1/orders");
            var contextMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            var httpClientMock = new Mock<IAcmeHttpClient>(MockBehavior.Strict);
            contextMock.SetupGet(c => c.HttpClient).Returns(httpClientMock.Object);
            httpClientMock
                .Setup(c => c.Get<OrderList>(location))
                .ReturnsAsync(new AcmeHttpResponse<OrderList>(
                    location,
                    new OrderList { Orders = new[] { new Uri("http://acme.d/order/1") } },
                    null,
                    null));

            var orders = await new OrderListContext(contextMock.Object, location).Orders();

            Assert.Single(orders);
            Assert.Equal(new Uri("http://acme.d/order/1"), orders.Single().Location);
            httpClientMock.Verify(c => c.Get<OrderList>(location), Times.Once);
        }
    }
}

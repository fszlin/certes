using System;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Moq;
using Xunit;

namespace Certes.Acme
{
    public class OrderContextTests
    {
        private Uri location = new Uri("http://acme.d/order/101");
        private Mock<IAcmeContext> contextMock = new Mock<IAcmeContext>();
        private Mock<IAcmeHttpClient> httpClientMock = new Mock<IAcmeHttpClient>();

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

            contextMock.Reset();
            httpClientMock.Reset();

            contextMock
                .Setup(c => c.GetDirectory())
                .ReturnsAsync(Helper.MockDirectoryV2);
            contextMock
                .SetupGet(c => c.AccountKey)
                .Returns(Helper.GetKeyV2());
            contextMock.SetupGet(c => c.HttpClient).Returns(httpClientMock.Object);
            httpClientMock
                .Setup(m => m.Get<Order>(location))
                .ReturnsAsync(new AcmeHttpResponse<Order>(location, order, default, default));

            var ctx = new OrderContext(contextMock.Object, location);
            var authzs = await ctx.Authorizations();
            Assert.Equal(order.Authorizations, authzs.Select(a => a.Location));

            // check the context returns empty list instead of null
            httpClientMock
                .Setup(m => m.Get<Order>(location))
                .ReturnsAsync(new AcmeHttpResponse<Order>(location, new Order(), default, default));
            authzs = await ctx.Authorizations();
            Assert.Empty(authzs);

        }
    }
}

using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Moq;
using Xunit;

namespace Certes.Acme
{
    public class EntityContextTests
    {
        [Fact]
        public async Task CanLoadResource()
        {
            var loc = new Uri("http://acme.d/acct/1");
            var acct = new Account();

            var httpMock = new Mock<IAcmeHttpClient>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.SetupGet(m => m.HttpClient).Returns(httpMock.Object);

            httpMock.Setup(m => m.Get<Account>(loc)).ReturnsAsync(new AcmeHttpResponse<Account>(loc, acct, default, default));
            var ctx = new EntityContext<Account>(ctxMock.Object, loc);

            var res = await ctx.Resource();
            Assert.Equal(acct, res);

            loc = new Uri("http://acme.d/acct/2");
            httpMock.Setup(m => m.Get<Account>(loc)).ReturnsAsync(new AcmeHttpResponse<Account>(loc, default, default, new AcmeError { Detail = "err" }));
            ctx = new EntityContext<Account>(ctxMock.Object, loc);
            await Assert.ThrowsAsync<AcmeRequestException>(() => ctx.Resource());
        }
    }
}

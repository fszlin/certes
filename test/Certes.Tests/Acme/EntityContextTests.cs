using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;
using Moq;
using Xunit;

namespace Certes.Acme
{
    public class EntityContextTests
    {
        [Fact]
        public async Task CanLoadResource()
        {
            var location = new Uri("http://acme.d/acct/1");
            var acct = new Account();

            var expectedPayload = new JwsSigner(Helper.GetKeyV2())
                .Sign("", null, location, "nonce");

            var httpMock = new Mock<IAcmeHttpClient>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.SetupGet(m => m.HttpClient).Returns(httpMock.Object);
            ctxMock
                .SetupGet(c => c.BadNonceRetryCount)
                .Returns(1);
            ctxMock
                .Setup(c => c.Sign(It.IsAny<object>(),  It.IsAny<Uri>()))
                .Callback((object payload, Uri loc) =>
                {
                    Assert.Null(payload);
                    Assert.Equal(location, loc);
                })
                .ReturnsAsync(expectedPayload);

            httpMock
                .Setup(m => m.Post<Account>(location, It.IsAny<JwsPayload>()))
                .Callback((Uri _, object o) =>
                {
                    var p = (JwsPayload)o;
                    Assert.Equal(expectedPayload.Payload, p.Payload);
                    Assert.Equal(expectedPayload.Protected, p.Protected);
                })
                .ReturnsAsync(new AcmeHttpResponse<Account>(location, acct, default, default));
            var ctx = new EntityContext<Account>(ctxMock.Object, location);

            var res = await ctx.Resource();
            Assert.Equal(acct, res);

            location = new Uri("http://acme.d/acct/2");
            httpMock
                .Setup(m => m.Post<Account>(location, It.IsAny<JwsPayload>()))
                .Callback((Uri _, object o) =>
                {
                    var p = (JwsPayload)o;
                    Assert.Equal(expectedPayload.Payload, p.Payload);
                    Assert.Equal(expectedPayload.Protected, p.Protected);
                })
                .ReturnsAsync(new AcmeHttpResponse<Account>(location, default, default, new AcmeError { Detail = "err" }));
            ctx = new EntityContext<Account>(ctxMock.Object, location);
            await Assert.ThrowsAsync<AcmeRequestException>(() => ctx.Resource());
        }
    }
}

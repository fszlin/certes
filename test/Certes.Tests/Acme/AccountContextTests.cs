using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Certes.Acme
{
    public class AccountContextTests
    {
        private Uri location = new Uri("http://acme.d/account/101");
        private Mock<IAcmeContext> contextMock = new Mock<IAcmeContext>();
        private Mock<IAcmeHttpClient> httpClientMock = new Mock<IAcmeHttpClient>();

        [Fact]
        public async Task CanDeactivateAccount()
        {
            var expectedPayload = new JwsPayload();
            var expectedAccount = new Account();

            contextMock.Reset();
            httpClientMock.Reset();

            contextMock
                .Setup(c => c.GetAccountLocation())
                .ReturnsAsync(location);
            contextMock
                .Setup(c => c.Sign(It.IsAny<object>(), location))
                .Callback((object payload, Uri loc) =>
                {
                    Assert.Equal(
                        JsonConvert.SerializeObject(new { status = AccountStatus.Deactivated }),
                        JsonConvert.SerializeObject(payload));
                    Assert.Equal(location, loc);
                })
                .ReturnsAsync(expectedPayload);
            contextMock.SetupGet(c => c.HttpClient).Returns(httpClientMock.Object);
            httpClientMock
                .Setup(c => c.Post<Account>(location, expectedPayload))
                .ReturnsAsync(new AcmeHttpResponse<Account>(location, expectedAccount, null, null));

            var instance = new AccountContext(contextMock.Object);
            var account = await instance.Deactivate();

            contextMock.Verify(c => c.GetAccountLocation(), Times.Once);
            httpClientMock.Verify(c => c.Post<Account>(location, expectedPayload), Times.Once);
            Assert.Equal(expectedAccount, account);
        }

        [Fact]
        public async Task CanLoadResource()
        {
            var expectedPayload = new JwsPayload();
            var expectedAccount = new Account();

            contextMock.Reset();
            httpClientMock.Reset();

            contextMock
                .Setup(c => c.GetAccountLocation())
                .ReturnsAsync(location);
            contextMock
                .Setup(c => c.Sign(It.IsAny<object>(), location))
                .Callback((object payload, Uri loc) =>
                {
                    Assert.Equal(
                        JsonConvert.SerializeObject(new { }),
                        JsonConvert.SerializeObject(payload));
                    Assert.Equal(location, loc);
                })
                .ReturnsAsync(expectedPayload);
            contextMock.SetupGet(c => c.HttpClient).Returns(httpClientMock.Object);
            httpClientMock
                .Setup(c => c.Post<Account>(location, expectedPayload))
                .ReturnsAsync(new AcmeHttpResponse<Account>(location, expectedAccount, null, null));

            var instance = new AccountContext(contextMock.Object);
            var account = await instance.Resource();

            contextMock.Verify(c => c.GetAccountLocation(), Times.Once);
            httpClientMock.Verify(c => c.Post<Account>(location, expectedPayload), Times.Once);
            Assert.Equal(expectedAccount, account);
        }
    }
}

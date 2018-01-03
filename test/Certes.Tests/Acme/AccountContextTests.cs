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
                .Setup(c => c.GetDirectory())
                .ReturnsAsync(Helper.MockDirectory);
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

            var instance = new AccountContext(contextMock.Object, location);
            var account = await instance.Deactivate();

            httpClientMock.Verify(c => c.Post<Account>(location, expectedPayload), Times.Once);
            Assert.Equal(expectedAccount, account);
        }

        [Fact]
        public async Task CanLoadResource()
        {
            var expectedAccount = new Account();

            var payload = new Account { OnlyReturnExisting = true };
            var expectedPayload = new JwsSigner(Helper.GetAccountKey())
                .Sign(payload, null, Helper.MockDirectory.NewAccount, "nonce");

            contextMock.Reset();
            httpClientMock.Reset();

            contextMock
                .Setup(c => c.GetDirectory())
                .ReturnsAsync(Helper.MockDirectory);
            contextMock
                .SetupGet(c => c.AccountKey)
                .Returns(Helper.GetAccountKey());
            contextMock.SetupGet(c => c.HttpClient).Returns(httpClientMock.Object);
            httpClientMock
                .Setup(c => c.ConsumeNonce())
                .ReturnsAsync("nonce");
            httpClientMock
                .Setup(c => c.Post<Account>(Helper.MockDirectory.NewAccount, It.IsAny<JwsPayload>()))
                .Callback((Uri _, object o) =>
                {
                    var p = (JwsPayload)o;
                    Assert.Equal(expectedPayload.Payload, p.Payload);
                    Assert.Equal(expectedPayload.Protected, p.Protected);
                })
                .ReturnsAsync(new AcmeHttpResponse<Account>(location, expectedAccount, null, null));

            var instance = new AccountContext(contextMock.Object, location);
            var account = await instance.Resource();

            Assert.Equal(expectedAccount, account);
        }
    }
}

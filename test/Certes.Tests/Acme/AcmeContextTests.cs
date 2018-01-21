using System;
using System.Text;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Moq;
using Xunit;

namespace Certes.Acme
{
    public class AcmeContextTests
    {
        [Fact]
        public void CanGetOrderByLocation()
        {
            var loc = new Uri("http://d.com/order/1");
            var ctx = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
            var order = ctx.Order(loc);

            Assert.Equal(loc, order.Location);
        }

        [Fact]
        public void CanGetAuthzByLocation()
        {
            var loc = new Uri("http://d.com/authz/1");
            var ctx = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
            var authz = ctx.Authorization(loc);

            Assert.Equal(loc, authz.Location);
        }

        [Fact]
        public async Task CanRevokeCertByPrivateKey()
        {
            var directoryUri = new Uri("http://acme.d/dict");
            var httpClientMock = new Mock<IAcmeHttpClient>();
            var certData = Encoding.UTF8.GetBytes("cert");

            httpClientMock.Setup(m => m.Get<Directory>(directoryUri))
                .ReturnsAsync(new AcmeHttpResponse<Directory>(directoryUri, Helper.MockDirectoryV2, default, default));
            httpClientMock.Setup(m => m.ConsumeNonce()).ReturnsAsync("nonce");

            httpClientMock.Setup(m => m.Post<string>(Helper.MockDirectoryV2.RevokeCert, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<string>(default, default, default, default));

            var certKey = KeyFactory.NewKey(KeyAlgorithm.ES256);

            var client = new AcmeContext(directoryUri, http: httpClientMock.Object);
            await client.RevokeCertificate(certData, RevocationReason.KeyCompromise, certKey);
        }
    }
}

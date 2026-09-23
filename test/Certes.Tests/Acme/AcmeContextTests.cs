using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Moq;
using Xunit;

using static Certes.Helper;
using Directory = Certes.Acme.Resource.Directory;

namespace Certes.Acme
{
    public class AcmeContextTests
    {
        [Fact]
        public async Task CanChangeKey()
        {
            var accountLoc = new Uri("https://acme.d/acct/1");
            var httpMock = new Mock<IAcmeHttpClient>();
            httpMock.Setup(m => m.Get<Directory>(It.IsAny<Uri>()))
                .ReturnsAsync(new AcmeHttpResponse<Directory>(
                    accountLoc, MockDirectoryV2, null, null));
            httpMock.Setup(
                m => m.Post<Account>(MockDirectoryV2.NewAccount, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<Account>(
                    accountLoc, new Account
                    {
                        Status = AccountStatus.Valid
                    }, null, null));
            httpMock.Setup(
                m => m.Post<Account>(MockDirectoryV2.KeyChange, It.IsAny<object>()))
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

            var newKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            await ctx.ChangeKey(newKey);
            Assert.Equal(newKey.ToPem(), ctx.AccountKey.ToPem());

            ctx = new AcmeContext(
                WellKnownServers.LetsEncryptStagingV2,
                key,
                httpMock.Object);
            await ctx.ChangeKey(null);
            Assert.NotEqual(key.ToPem(), ctx.AccountKey.ToPem());
        }

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

        [Fact]
        public async Task CanCreateARICertificateId()
        {
            const string filePath = "./Sample/example.pfx.base64";
            var base64Pfx = await File.ReadAllTextAsync(filePath);
            var pfxBytes = Convert.FromBase64String(base64Pfx);

            var result = AcmeContext.GetAriCertificateId(pfxBytes, "abc123");

            Assert.Equal("MBaAFCc2NSiyPMHMMB9tRrZeS+Y963by.B1vNFQ", result);
        }
    }
}

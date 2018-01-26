using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Moq;
using Xunit;

namespace Certes
{
    public class IOrderContextExtensionsTests
    {
        [Fact]
        public async Task CanGenerateCertificate()
        {
            var pem = File.ReadAllText("./Data/cert-es256.pem");

            var orderCtxMock = new Mock<IOrderContext>();
            orderCtxMock.Setup(m => m.Download()).ReturnsAsync(new CertificateChain(pem));
            orderCtxMock.Setup(m => m.Resource()).ReturnsAsync(new Order
            {
                Identifiers = new[] {
                    new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                },
                Status = OrderStatus.Pending,
            });
            orderCtxMock.Setup(m => m.Finalize(It.IsAny<byte[]>()))
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Valid,
                });
            
            var certInfoWithRandomKey = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
                CommonName = "www.certes.com",
            });

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfoWithRandomKey.ToPem().Where(c => !char.IsWhiteSpace(c)));
            Assert.NotNull(certInfoWithRandomKey.PrivateKey);

            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var certInfo = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
                CommonName = "www.certes.com",
            }, key);

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfo.ToPem().Where(c => !char.IsWhiteSpace(c)));
            Assert.Equal(key, certInfo.PrivateKey);

            var certInfoNoCn = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
            });

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfoNoCn.ToPem().Where(c => !char.IsWhiteSpace(c)));
            Assert.NotNull(certInfoNoCn.PrivateKey);
        }
    }

}

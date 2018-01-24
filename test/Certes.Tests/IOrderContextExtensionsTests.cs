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
            var pem = "certes-pem";

            var orderCtxMock = new Mock<IOrderContext>();
            orderCtxMock.Setup(m => m.Download()).ReturnsAsync(pem);
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

            Assert.Equal(pem, certInfoWithRandomKey.ToPem());
            Assert.NotNull(certInfoWithRandomKey.PrivateKey);

            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var certInfo = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
                CommonName = "www.certes.com",
            }, key);

            Assert.Equal(pem, certInfo.ToPem());
            Assert.Equal(key, certInfo.PrivateKey);

            var certInfoNoCn = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
            });

            Assert.Equal(pem, certInfoNoCn.ToPem());
            Assert.NotNull(certInfoNoCn.PrivateKey);
        }
    }

}

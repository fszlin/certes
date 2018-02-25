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
            
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var certInfo = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
                CommonName = "www.certes.com",
            }, key);

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfo.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));

            var certInfoNoCn = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
            }, key);

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfoNoCn.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));
        }
    }

}

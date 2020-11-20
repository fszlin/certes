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
        public async Task CanGenerateCertificateWhenOrderReady()
        {
            var pem = File.ReadAllText("./Data/cert-es256.pem");

            var orderCtxMock = new Mock<IOrderContext>();
            orderCtxMock.Setup(m => m.Download(null)).ReturnsAsync(new CertificateChain(pem));
            orderCtxMock.Setup(m => m.Resource()).ReturnsAsync(new Order
            {
                Identifiers = new[] {
                    new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                },
                Status = OrderStatus.Ready,
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
            }, key, null);

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfo.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));

            var certInfoNoCn = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
            }, key, null);

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfoNoCn.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));
        }

        [Fact]
        public async Task CanGenerateCertificateWhenOrderPending()
        {
            var pem = File.ReadAllText("./Data/cert-es256.pem");

            var orderCtxMock = new Mock<IOrderContext>();
            orderCtxMock.Setup(m => m.Download(null)).ReturnsAsync(new CertificateChain(pem));
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
            }, key, null);

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfo.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));

            var certInfoNoCn = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
            }, key, null);

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfoNoCn.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));
        }

        [Fact]
        public async Task CanGenerateCertificateWhenOrderProcessing()
        {
            var pem = File.ReadAllText("./Data/cert-es256.pem");

            var orderCtxMock = new Mock<IOrderContext>();
            orderCtxMock.Setup(m => m.Download(null)).ReturnsAsync(new CertificateChain(pem));
            orderCtxMock.SetupSequence(m => m.Resource())
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Ready,
                })
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Ready,
                })
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Processing,
                })
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Valid,
                })
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Ready,
                })
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Ready,
                })
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Valid,
                });
            orderCtxMock.Setup(m => m.Finalize(It.IsAny<byte[]>()))
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Processing,
                });

            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var certInfo = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
                CommonName = "www.certes.com",
            }, key, null, 5);

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

            orderCtxMock.Verify(m => m.Resource(), Times.Exactly(7));
        }


        [Fact]
        public async Task CanGenerateWithAlternateLink()
        {
            var defaultpem = File.ReadAllText("./Data/defaultLeaf.pem");
            var alternatepem = File.ReadAllText("./Data/alternateLeaf.pem");

            var accountLoc = new System.Uri("http://acme.d/account/101");
            var orderLoc = new System.Uri("http://acme.d/order/101");
            var finalizeLoc = new System.Uri("http://acme.d/order/101/finalize");
            var certDefaultLoc = new System.Uri("http://acme.d/order/101/cert/1234");
            var certAlternateLoc = new System.Uri("http://acme.d/order/101/cert/1234/1");

            var alternates = new [] {
                new {key = "alternate", value = certDefaultLoc},
                new {key = "alternate", value = certAlternateLoc},
            }.ToLookup(x => x.key, x => x.value);

            var httpClientMock = new Mock<IAcmeHttpClient>();

            httpClientMock.Setup(m => m.Post<string>(certDefaultLoc, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<string>(
                    accountLoc, 
                    defaultpem,
                    alternates, 
                    null));

            httpClientMock.Setup(m => m.Post<string>(certAlternateLoc, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<string>(
                    accountLoc, 
                    alternatepem,
                    alternates, 
                    null));

            httpClientMock.Setup(m => m.Post<Order>(finalizeLoc, It.IsAny<object>()))
                .ReturnsAsync(new AcmeHttpResponse<Order>(
                    accountLoc, 
                    new Order
                    {
                        Identifiers = new[] {
                            new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                        },
                        Status = OrderStatus.Valid,
                    },
                    null, 
                    null));


            var acmeContextMock = new Mock<IAcmeContext>();
            acmeContextMock.SetupGet(x => x.HttpClient)
                .Returns(httpClientMock.Object);

            var orderCtxMock = new Mock<OrderContext>(acmeContextMock.Object, orderLoc);
            orderCtxMock.Setup(m => m.Resource()).ReturnsAsync(new Order
            {
                Identifiers = new[] {
                    new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                },
                Certificate = certDefaultLoc,
                Finalize = finalizeLoc,
                Status = OrderStatus.Pending,
            });
            
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var certInfoDefaultRoot = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
                CommonName = "www.certes.com",
            }, key, null);

            Assert.Equal(
                defaultpem.Where(c => !char.IsWhiteSpace(c)),
                certInfoDefaultRoot.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));

            var certInfoAlternateRoot = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
                CommonName = "www.certes.com",
            }, key, "AlternateRoot");

            Assert.Equal(
                alternatepem.Where(c => !char.IsWhiteSpace(c)),
                certInfoAlternateRoot.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));

            var certInfoUnknownRoot = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "C",
                CommonName = "www.certes.com",
            }, key, "UnknownRoot");

            Assert.Equal(
                defaultpem.Where(c => !char.IsWhiteSpace(c)),
                certInfoUnknownRoot.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));

        }

        [Fact]
        public async Task ThrowWhenOrderNotReady()
        {
            var orderCtxMock = new Mock<IOrderContext>();

            orderCtxMock.Setup(m => m.Resource()).ReturnsAsync(new Order
            {
                Identifiers = new[] {
                    new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                },
                Status = OrderStatus.Valid,
            });

            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            await Assert.ThrowsAsync<AcmeException>(() =>
                orderCtxMock.Object.Generate(new CsrInfo
                {
                    CountryName = "C",
                    CommonName = "www.certes.com",
                }, key, null));
        }

        [Fact]
        public async Task ThrowWhenFinalizeFailed()
        {
            var pem = File.ReadAllText("./Data/cert-es256.pem");

            var orderCtxMock = new Mock<IOrderContext>();
            orderCtxMock.Setup(m => m.Download(null)).ReturnsAsync(new CertificateChain(pem));
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
                    Status = OrderStatus.Invalid,
                });

            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            await Assert.ThrowsAsync<AcmeException>(() =>
                orderCtxMock.Object.Generate(new CsrInfo
                {
                    CountryName = "C",
                    CommonName = "www.certes.com",
                }, key, null));
        }

        [Fact]
        public async Task ThrowWhenProcessintTooOften()
        {
            var pem = File.ReadAllText("./Data/cert-es256.pem");

            var orderCtxMock = new Mock<IOrderContext>();
            orderCtxMock.Setup(m => m.Download(null)).ReturnsAsync(new CertificateChain(pem));
            orderCtxMock.SetupSequence(m => m.Resource())
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Ready,
                })
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Ready,
                })
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Processing,
                })
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Processing,
                });

            orderCtxMock.Setup(m => m.Finalize(It.IsAny<byte[]>()))
                .ReturnsAsync(new Order
                {
                    Identifiers = new[] {
                        new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                    },
                    Status = OrderStatus.Processing,
                });

            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            await Assert.ThrowsAsync<AcmeException>(() =>
                orderCtxMock.Object.Generate(new CsrInfo
                {
                    CountryName = "C",
                    CommonName = "www.certes.com",
                }, key));
        }

    }

}

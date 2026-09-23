using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
                CountryName = "CA",
                CommonName = "www.certes.com",
            }, key, null);

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfo.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));

            var certInfoNoCn = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "CA",
            }, key, null);

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfoNoCn.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));
        }

        [Fact]
        public async Task CanGenerateCertificateWhenOrderPending()
        {
            var pem = File.ReadAllText("./Data/cert-es256.pem");

            var pendingOrder = new Order
            {
                Identifiers = new[] {
                    new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                },
                Status = OrderStatus.Pending,
            };
            var readyOrder = new Order
            {
                Identifiers = pendingOrder.Identifiers,
                Status = OrderStatus.Ready,
            };

            var orderCtxMock = new Mock<IOrderContext>();
            orderCtxMock.Setup(m => m.Download(null)).ReturnsAsync(new CertificateChain(pem));
            orderCtxMock.SetupSequence(m => m.Resource())
                .ReturnsAsync(pendingOrder)
                .ReturnsAsync(readyOrder)
                .ReturnsAsync(readyOrder)
                .ReturnsAsync(pendingOrder)
                .ReturnsAsync(readyOrder)
                .ReturnsAsync(readyOrder);
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
                CountryName = "CA",
                CommonName = "www.certes.com",
            }, key, null);

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfo.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));

            var certInfoNoCn = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "CA",
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
                CountryName = "CA",
                CommonName = "www.certes.com",
            }, key, null, 5);

            Assert.Equal(
                pem.Where(c => !char.IsWhiteSpace(c)),
                certInfo.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));

            var certInfoNoCn = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "CA",
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
                Status = OrderStatus.Ready,
            });
            
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var certInfoDefaultRoot = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "CA",
                CommonName = "www.certes.com",
            }, key, null);

            Assert.Equal(
                defaultpem.Where(c => !char.IsWhiteSpace(c)),
                certInfoDefaultRoot.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));

            var certInfoAlternateRoot = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "CA",
                CommonName = "www.certes.com",
            }, key, "AlternateRoot");

            Assert.Equal(
                alternatepem.Where(c => !char.IsWhiteSpace(c)),
                certInfoAlternateRoot.Certificate.ToPem().Where(c => !char.IsWhiteSpace(c)));

            var certInfoUnknownRoot = await orderCtxMock.Object.Generate(new CsrInfo
            {
                CountryName = "CA",
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
                    CountryName = "CA",
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
                    CountryName = "CA",
                    CommonName = "www.certes.com",
                }, key, null));
        }

        [Fact]
        public async Task PollsPendingAndProcessingUsingServerRetryAfterDelay()
        {
            var pem = File.ReadAllText("./Data/cert-es256.pem");
            var ready = new Order
            {
                Identifiers = new[] { new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns } },
                Status = OrderStatus.Ready,
            };
            var pending = new Order
            {
                Identifiers = ready.Identifiers,
                Status = OrderStatus.Pending,
            };
            var valid = new Order
            {
                Identifiers = ready.Identifiers,
                Status = OrderStatus.Valid,
            };
            var orderCtxMock = new Mock<IOrderContext>();
            var resourceCalls = 0;
            orderCtxMock
                .Setup(m => m.Resource())
                .ReturnsAsync(() => ++resourceCalls <= 2 ? ready : resourceCalls <= 4 ? pending : valid);
            orderCtxMock.Setup(m => m.Finalize(It.IsAny<byte[]>())).ReturnsAsync(new Order
            {
                Identifiers = ready.Identifiers,
                Status = OrderStatus.Processing,
            });
            orderCtxMock.SetupSequence(m => m.RetryAfter)
                .Returns(120)
                .Returns(0)
                .Returns(int.MaxValue);
            orderCtxMock.Setup(m => m.Download(null)).ReturnsAsync(new CertificateChain(pem));

            var delays = new List<TimeSpan>();
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            await IOrderContextExtensions.Generate(
                orderCtxMock.Object,
                new CsrInfo { CommonName = "www.certes.com" },
                key,
                null,
                3,
                delay =>
                {
                    delays.Add(delay);
                    return Task.CompletedTask;
                });

            Assert.Equal(3, delays.Count);
            Assert.Equal(TimeSpan.FromSeconds(120), delays[0]);
            Assert.Equal(TimeSpan.FromSeconds(1), delays[1]);
            Assert.Equal(TimeSpan.FromMinutes(15), delays[2]);
            Assert.Equal(5, resourceCalls);
        }

        [Fact]
        public async Task HonorsCallerPollingBudget()
        {
            var order = new Order
            {
                Identifiers = new[] { new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns } },
                Status = OrderStatus.Ready,
            };
            var processing = new Order
            {
                Identifiers = order.Identifiers,
                Status = OrderStatus.Processing,
            };
            var orderCtxMock = new Mock<IOrderContext>();
            var resourceCalls = 0;
            orderCtxMock.Setup(m => m.Resource()).ReturnsAsync(() => ++resourceCalls <= 2 ? order : processing);
            orderCtxMock.Setup(m => m.Finalize(It.IsAny<byte[]>())).ReturnsAsync(processing);
            orderCtxMock.SetupGet(m => m.RetryAfter).Returns(0);

            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            await Assert.ThrowsAsync<AcmeException>(() => IOrderContextExtensions.Generate(
                orderCtxMock.Object,
                new CsrInfo { CommonName = "www.certes.com" },
                key,
                null,
                100,
                _ => Task.CompletedTask));

            Assert.Equal(102, resourceCalls);
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
            await Assert.ThrowsAsync<AcmeException>(() => IOrderContextExtensions.Generate(
                orderCtxMock.Object,
                new CsrInfo
                {
                    CountryName = "CA",
                    CommonName = "www.certes.com",
                },
                key,
                null,
                1,
                _ => Task.CompletedTask));
        }

        [Fact]
        public async Task StopsPollingWhenOrderBecomesInvalid()
        {
            var order = new Order
            {
                Identifiers = new[] { new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns } },
                Status = OrderStatus.Ready,
            };
            var pending = new Order
            {
                Identifiers = order.Identifiers,
                Status = OrderStatus.Pending,
            };
            var invalid = new Order
            {
                Identifiers = order.Identifiers,
                Status = OrderStatus.Invalid,
            };

            var orderCtxMock = new Mock<IOrderContext>();
            var resourceCalls = 0;
            orderCtxMock.Setup(m => m.Resource()).ReturnsAsync(() => ++resourceCalls <= 2 ? order : resourceCalls == 3 ? pending : invalid);
            orderCtxMock.Setup(m => m.Finalize(It.IsAny<byte[]>())).ReturnsAsync(pending);
            orderCtxMock.SetupGet(m => m.RetryAfter).Returns(5);

            var delays = new List<TimeSpan>();
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);

            await Assert.ThrowsAsync<AcmeException>(() => IOrderContextExtensions.Generate(
                orderCtxMock.Object,
                new CsrInfo { CommonName = "www.certes.com" },
                key,
                null,
                10,
                delay =>
                {
                    delays.Add(delay);
                    return Task.CompletedTask;
                }));

            Assert.Equal(2, delays.Count);
            Assert.Equal(4, resourceCalls);
        }

        [Fact]
        public async Task TreatsNegativePollingBudgetAsZero()
        {
            var order = new Order
            {
                Identifiers = new[] { new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns } },
                Status = OrderStatus.Ready,
            };
            var processing = new Order
            {
                Identifiers = order.Identifiers,
                Status = OrderStatus.Processing,
            };

            var orderCtxMock = new Mock<IOrderContext>();
            var resourceCalls = 0;
            orderCtxMock.Setup(m => m.Resource()).ReturnsAsync(() => ++resourceCalls <= 1 ? order : processing);
            orderCtxMock.Setup(m => m.Finalize(It.IsAny<byte[]>())).ReturnsAsync(processing);

            var delayCalled = false;
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);

            await Assert.ThrowsAsync<AcmeException>(() => IOrderContextExtensions.Generate(
                orderCtxMock.Object,
                new CsrInfo { CommonName = "www.certes.com" },
                key,
                null,
                -5,
                _ =>
                {
                    delayCalled = true;
                    return Task.CompletedTask;
                }));

            Assert.False(delayCalled);
            Assert.Equal(2, resourceCalls);
        }

        [Fact]
        public async Task SucceedsWhenOrderBecomesValidWithinPollingBudget()
        {
            var pem = File.ReadAllText("./Data/cert-es256.pem");
            var ready = new Order
            {
                Identifiers = new[] { new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns } },
                Status = OrderStatus.Ready,
            };
            var processing = new Order
            {
                Identifiers = ready.Identifiers,
                Status = OrderStatus.Processing,
            };
            var valid = new Order
            {
                Identifiers = ready.Identifiers,
                Status = OrderStatus.Valid,
            };

            var orderCtxMock = new Mock<IOrderContext>();
            var resourceCalls = 0;
            orderCtxMock.Setup(m => m.Resource()).ReturnsAsync(() => ++resourceCalls <= 1 ? ready : resourceCalls == 2 ? processing : valid);
            orderCtxMock.Setup(m => m.Finalize(It.IsAny<byte[]>())).ReturnsAsync(processing);
            orderCtxMock.SetupGet(m => m.RetryAfter).Returns(2);
            orderCtxMock.Setup(m => m.Download(null)).ReturnsAsync(new CertificateChain(pem));

            var delays = new List<TimeSpan>();
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);

            var result = await IOrderContextExtensions.Generate(
                orderCtxMock.Object,
                new CsrInfo { CommonName = "www.certes.com" },
                key,
                null,
                2,
                delay =>
                {
                    delays.Add(delay);
                    return Task.CompletedTask;
                });

            Assert.NotNull(result);
            Assert.Single(delays);
            Assert.Equal(3, resourceCalls);
        }

        [Fact]
        public async Task CanRecoverWhenFinalizeReturnsNullAndOrderLaterBecomesValid()
        {
            var pem = File.ReadAllText("./Data/cert-es256.pem");
            var ready = new Order
            {
                Identifiers = new[] { new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns } },
                Status = OrderStatus.Ready,
            };
            var pending = new Order
            {
                Identifiers = ready.Identifiers,
                Status = OrderStatus.Pending,
            };
            var valid = new Order
            {
                Identifiers = ready.Identifiers,
                Status = OrderStatus.Valid,
            };

            var orderCtxMock = new Mock<IOrderContext>();
            var resourceCalls = 0;
            orderCtxMock.Setup(m => m.Resource()).ReturnsAsync(() => ++resourceCalls <= 1 ? ready : resourceCalls == 2 ? pending : valid);
            orderCtxMock.Setup(m => m.Finalize(It.IsAny<byte[]>())).ReturnsAsync((Order)null);
            orderCtxMock.SetupGet(m => m.RetryAfter).Returns(1);
            orderCtxMock.Setup(m => m.Download(null)).ReturnsAsync(new CertificateChain(pem));

            var delays = new List<TimeSpan>();
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            var chain = await IOrderContextExtensions.Generate(
                orderCtxMock.Object,
                new CsrInfo { CommonName = "www.certes.com" },
                key,
                null,
                2,
                delay =>
                {
                    delays.Add(delay);
                    return Task.CompletedTask;
                });

            Assert.NotNull(chain);
            Assert.Single(delays);
            Assert.Equal(3, resourceCalls);
        }

        [Fact]
        public async Task PollingBubblesTransientNetworkError()
        {
            var order = new Order
            {
                Identifiers = new[] { new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns } },
                Status = OrderStatus.Ready,
            };
            var processing = new Order
            {
                Identifiers = order.Identifiers,
                Status = OrderStatus.Processing,
            };

            var orderCtxMock = new Mock<IOrderContext>();
            orderCtxMock
                .SetupSequence(m => m.Resource())
                .ReturnsAsync(order)
                .ReturnsAsync(order)
                .ThrowsAsync(new HttpRequestException("transient"));
            orderCtxMock.Setup(m => m.Finalize(It.IsAny<byte[]>())).ReturnsAsync(processing);
            orderCtxMock.SetupGet(m => m.RetryAfter).Returns(3);

            var delays = new List<TimeSpan>();
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);

            await Assert.ThrowsAsync<HttpRequestException>(() => IOrderContextExtensions.Generate(
                orderCtxMock.Object,
                new CsrInfo { CommonName = "www.certes.com" },
                key,
                null,
                5,
                delay =>
                {
                    delays.Add(delay);
                    return Task.CompletedTask;
                }));

            Assert.Single(delays);
            Assert.Equal(TimeSpan.FromSeconds(3), delays[0]);
        }

    }

}

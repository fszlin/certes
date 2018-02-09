using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Options;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Certes.Cli.Processors
{
    [Collection(nameof(ContextFactory))]
    public class OrderCommandTests
    {
        [Fact]
        public void CanParseCommnad()
        {
            var options = Parse("noop");
            Assert.Null(options);

            // list order
            options = Parse("order list");
            Assert.Equal(OrderAction.List, options.Action);

            // show order by location
            options = Parse("order --uri http://acme.d/order/1");
            Assert.Equal(OrderAction.Info, options.Action);

            // show order by location
            options = Parse("order list");
            Assert.Equal(OrderAction.List, options.Action);

            // new order with names
            options = Parse("order new www.certes.com mail.certes.com");
            Assert.Equal(OrderAction.New, options.Action);
            Assert.Equal(new[] { "www.certes.com", "mail.certes.com" }, options.Values);

            // show authzs
            options = Parse("order authz www.certes.com --uri http://acme.d/order/1");
            Assert.Equal(OrderAction.Authz, options.Action);
            Assert.Equal(new Uri("http://acme.d/order/1"), options.Location);
            Assert.Equal(AuthorizationType.Unspecific, options.Validate);
            Assert.Equal(new[] { "www.certes.com" }, options.Values);

            // validate authz
            options = Parse("order authz www.certes.com --uri http://acme.d/order/1 --validate dns");
            Assert.Equal(OrderAction.Authz, options.Action);
            Assert.Equal(new Uri("http://acme.d/order/1"), options.Location);
            Assert.Equal(AuthorizationType.Dns, options.Validate);
            Assert.Equal(new[] { "www.certes.com" }, options.Values);

            // finalize order
            options = Parse("order finalize --uri http://acme.d/order/1 --dn CN=www.certes.com,C=Canada --cert-key ./cert-key.pem");
            Assert.Equal(OrderAction.Finalize, options.Action);
            Assert.Equal(new Uri("http://acme.d/order/1"), options.Location);
            Assert.Equal("CN=www.certes.com,C=Canada", options.DistinguishName);
            Assert.Equal("./cert-key.pem", options.CertKeyPath);
        }

        [Fact]
        public async Task CanFinalizeOrder()
        {
            var keyPath = $"./Data/{nameof(CanFinalizeOrder)}/key.pem";
            var certKeyPath = $"./Data/{nameof(CanFinalizeOrder)}/cert-key.pem";
            Helper.SaveKey(keyPath);
            if (File.Exists(certKeyPath))
            {
                File.Delete(certKeyPath);
            }

            var hosts = new List<string> { "www.certes.com" };
            var order = new
            {
                uri = new Uri("http://acme.d/order/1"),
                data = new Order
                {
                    Identifiers = hosts.Select(h => new Identifier { Value = h, Type = IdentifierType.Dns }).ToArray(),
                    Authorizations = hosts.Select((i, a) => new Uri("http://acme.d/authz/{i}")).ToArray(),
                },
                certKey = (string)null,
            };

            var orderMock = new Mock<IOrderContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.SetupGet(c => c.AccountKey).Returns(Helper.GetKeyV2());
            ctxMock.Setup(c => c.Order(order.uri)).Returns(orderMock.Object);

            orderMock.Setup(c => c.Location).Returns(order.uri);
            orderMock.Setup(m => m.Finalize(It.IsAny<byte[]>()))
                .ReturnsAsync(order.data);

            ContextFactory.Create = (uri, key) => ctxMock.Object;
            var options = new OrderOptions
            {
                Action = OrderAction.Finalize,
                Location = order.uri,
                Path = keyPath,
                DistinguishName = "CN=www.certes.com,C=Canada",
                CertKeyPath = certKeyPath,
            };

            var proc = new OrderCommand(options);

            dynamic ret = await proc.Process();
            Assert.Equal(JsonConvert.SerializeObject(order), JsonConvert.SerializeObject(ret));
            Assert.True(File.Exists(options.CertKeyPath));

            options = new OrderOptions
            {
                Action = OrderAction.Finalize,
                Location = order.uri,
                Path = keyPath,
                DistinguishName = "CN=www.certes.com,C=Canada",
                CertKeyPath = certKeyPath,
            };

            // use speific key for CSR
            proc = new OrderCommand(options);
            ret = await proc.Process();
            Assert.Null(ret.certKey);

            File.Delete(certKeyPath);
            options = new OrderOptions
            {
                Action = OrderAction.Finalize,
                Location = order.uri,
                Path = keyPath,
                DistinguishName = "CN=www.certes.com,C=Canada",
            };

            // result should contain the generated key
            proc = new OrderCommand(options);
            ret = await proc.Process();
            Assert.NotNull(ret.certKey);
        }

        [Fact]
        public async Task CanListOrder()
        {
            var keyPath = $"./Data/{nameof(CanListOrder)}/key.pem";
            Helper.SaveKey(keyPath);

            var hosts = new List<string> { "www.certes.com", "mail.certes.com" };
            var order = new
            {
                uri = new Uri("http://acme.d/order/1"),
                data = new Order
                {
                    Identifiers = hosts.Select(h => new Identifier { Value = h, Type = IdentifierType.Dns }).ToArray(),
                    Authorizations = hosts.Select((i, a) => new Uri("http://acme.d/authz/{i}")).ToArray(),
                },
            };

            var orderMock = new Mock<IOrderContext>();
            var accountMock = new Mock<IAccountContext>();
            var orderListMock = new Mock<IOrderListContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.SetupGet(m => m.AccountKey).Returns(Helper.GetKeyV2());
            ctxMock.Setup(m => m.Account()).ReturnsAsync(accountMock.Object);

            orderMock.Setup(m => m.Location).Returns(order.uri);
            orderMock.Setup(m => m.Resource()).ReturnsAsync(order.data);

            accountMock.Setup(m => m.Orders()).ReturnsAsync(orderListMock.Object);

            orderListMock.Setup(m => m.Orders()).ReturnsAsync(new[] { orderMock.Object, orderMock.Object });

            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var proc = new OrderCommand(new OrderOptions
            {
                Action = OrderAction.List,
                Path = keyPath,
            });

            var ret = await proc.Process();
            Assert.Equal(
                JsonConvert.SerializeObject(new[] { order, order }),
                JsonConvert.SerializeObject(ret));
        }

        [Fact]
        public async Task CanShowOrder()
        {
            var keyPath = $"./Data/{nameof(CanShowOrder)}/key.pem";
            Helper.SaveKey(keyPath);

            var hosts = new List<string> { "www.certes.com", "mail.certes.com" };
            var order = new
            {
                uri = new Uri("http://acme.d/order/1"),
                data = new Order
                {
                    Identifiers = hosts.Select(h => new Identifier { Value = h, Type = IdentifierType.Dns }).ToArray(),
                    Authorizations = hosts.Select((i, a) => new Uri("http://acme.d/authz/{i}")).ToArray(),
                },
            };

            var orderMock = new Mock<IOrderContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.SetupGet(c => c.AccountKey).Returns(Helper.GetKeyV2());
            ctxMock.Setup(c => c.Order(order.uri)).Returns(orderMock.Object);

            orderMock.Setup(c => c.Location).Returns(order.uri);
            orderMock.Setup(c => c.Resource()).ReturnsAsync(order.data);

            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var proc = new OrderCommand(new OrderOptions
            {
                Action = OrderAction.Info,
                Location = order.uri,
                Path = keyPath,
            });

            var ret = await proc.Process();
            Assert.Equal(JsonConvert.SerializeObject(order), JsonConvert.SerializeObject(ret));
        }

        [Fact]
        public async Task CanProcessAuthz()
        {
            var keyPath = $"./Data/{nameof(CanProcessAuthz)}/key.pem";
            Helper.SaveKey(keyPath);

            var hosts = new List<string> { "www.certes.com", "mail.certes.com" };
            var order = new
            {
                uri = new Uri("http://acme.d/order/1"),
                data = new Order
                {
                    Identifiers = hosts.Select(h => new Identifier { Value = h, Type = IdentifierType.Dns }).ToArray(),
                    Authorizations = hosts.Select((i, a) => new Uri("http://acme.d/authz/{i}")).ToArray(),
                },
            };

            var authz = new
            {
                uri = order.data.Authorizations[0],
                data = new Acme.Resource.Authorization
                {
                    Identifier = new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                }
            };

            var authzMock = new Mock<IAuthorizationContext>();
            var orderMock = new Mock<IOrderContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.SetupGet(c => c.AccountKey).Returns(Helper.GetKeyV2());
            ctxMock.Setup(c => c.Order(order.uri)).Returns(orderMock.Object);

            orderMock.Setup(c => c.Authorizations()).ReturnsAsync(order.data.Identifiers.Select(a => authzMock.Object));
            orderMock.Setup(c => c.Location).Returns(order.uri);

            authzMock.Setup(c => c.Location).Returns(order.data.Authorizations[0]);
            authzMock.Setup(c => c.Resource()).ReturnsAsync(authz.data);

            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var proc = new OrderCommand(new OrderOptions
            {
                Action = OrderAction.Authz,
                Values = hosts,
                Location = order.uri,
                Path = keyPath,
            });

            var ret = await proc.Process();
            Assert.Equal(JsonConvert.SerializeObject(authz), JsonConvert.SerializeObject(ret));
        }
        
        [Theory]
        [InlineData(AuthorizationType.Dns, "dns-01")]
        [InlineData(AuthorizationType.Http, "http-01")]
        public async Task CanValidateAuthz(AuthorizationType authzType, string authzTypeString)
        {
            var keyPath = $"./Data/{nameof(CanValidateAuthz)}/key.pem";
            Helper.SaveKey(keyPath);

            var hosts = new List<string> { "www.certes.com", "mail.certes.com" };
            var order = new
            {
                uri = new Uri("http://acme.d/order/1"),
                data = new Order
                {
                    Identifiers = hosts.Select(h => new Identifier { Value = h, Type = IdentifierType.Dns }).ToArray(),
                    Authorizations = hosts.Select((i, a) => new Uri("http://acme.d/authz/{i}")).ToArray(),
                },
            };

            var authz = new
            {
                uri = order.data.Authorizations[0],
                data = new Acme.Resource.Authorization
                {
                    Identifier = new Identifier { Value = "www.certes.com", Type = IdentifierType.Dns },
                }
            };

            var challengeMock = new Mock<IChallengeContext>();
            var authzMock = new Mock<IAuthorizationContext>();
            var orderMock = new Mock<IOrderContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.SetupGet(c => c.AccountKey).Returns(Helper.GetKeyV2());
            ctxMock.Setup(c => c.Order(order.uri)).Returns(orderMock.Object);

            orderMock.Setup(c => c.Authorizations()).ReturnsAsync(order.data.Identifiers.Select(a => authzMock.Object));
            orderMock.Setup(c => c.Location).Returns(order.uri);

            challengeMock.SetupGet(c => c.Type).Returns(authzTypeString);

            authzMock.Setup(c => c.Challenges()).ReturnsAsync(new[] { challengeMock.Object });
            authzMock.Setup(c => c.Location).Returns(order.data.Authorizations[0]);
            authzMock.Setup(c => c.Resource()).ReturnsAsync(authz.data);

            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var proc = new OrderCommand(new OrderOptions
            {
                Action = OrderAction.Authz,
                Values = hosts,
                Location = order.uri,
                Validate = authzType,
                Path = keyPath,
            });

            var ret = await proc.Process();
            challengeMock.Verify(c => c.Validate(), Times.Once);
            Assert.Equal(JsonConvert.SerializeObject(authz), JsonConvert.SerializeObject(ret));
        }

        [Fact]
        public async Task CanNewOrder()
        {
            var keyPath = $"./Data/{nameof(CanNewOrder)}/key.pem";
            Helper.SaveKey(keyPath);

            var hosts = new List<string> { "www.certes.com", "mail.certes.com" };
            var order = new
            {
                uri = new Uri("http://acme.d/order/1"),
                data = new Order()
            };

            var orderMock = new Mock<IOrderContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.SetupGet(c => c.AccountKey).Returns(Helper.GetKeyV2());
            ctxMock.Setup(c => c.NewOrder(It.IsAny<IList<string>>(), null, null))
                .ReturnsAsync(orderMock.Object);
            orderMock.Setup(c => c.Resource()).ReturnsAsync(order.data);
            orderMock.Setup(c => c.Location).Returns(order.uri);
            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var proc = new OrderCommand(new OrderOptions
            {
                Action = OrderAction.New,
                Values = hosts.AsReadOnly(),
                Path = keyPath,
            });

            var ret = await proc.Process();
            Assert.Equal(JsonConvert.SerializeObject(order), JsonConvert.SerializeObject(ret));
        }

        [Fact]
        public async Task InvalidAction()
        {
            var proc = new OrderCommand(new OrderOptions
            {
                Action = (OrderAction)int.MaxValue,
            });

            await Assert.ThrowsAsync<NotSupportedException>(() => proc.Process());
        }

        private OrderOptions Parse(string cmd)
        {
            OrderOptions options = null;
            ArgumentSyntax.Parse(cmd.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                options = OrderCommand.TryParse(syntax);
            });

            return options;
        }
    }
}

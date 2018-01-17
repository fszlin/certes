#if NETCOREAPP1_0 || NETCOREAPP2_0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
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

            options = Parse("order");
            Assert.Equal(OrderAction.List, options.Action);

            options = Parse("order new www.certes.com mail.certes.com");
            Assert.Equal(OrderAction.New, options.Action);
            Assert.Equal(new[] { "www.certes.com", "mail.certes.com" }, options.Values);
        }

        [Fact]
        public async Task CanNewOrder()
        {
            var keyPath = $"./Data/{nameof(CanNewOrder)}/key.pem";
            Helper.SaveKey(keyPath);

            var hosts = new List<string> { "www.certes.com", "mail.certes.com" };
            var order = new Order();

            var orderMock = new Mock<IOrderContext>();
            var ctxMock = new Mock<IAcmeContext>();
            ctxMock.SetupGet(c => c.AccountKey).Returns(Helper.GetKeyV2());
            ctxMock.Setup(c => c.NewOrder(It.IsAny<IList<string>>(), null, null))
                .ReturnsAsync(orderMock.Object);
            orderMock.Setup(c => c.Resource()).ReturnsAsync(order);
            ContextFactory.Create = (uri, key) => ctxMock.Object;

            var proc = new OrderCommand(new OrderOptions
            {
                Action = OrderAction.New,
                Values = hosts.AsReadOnly(),
                Path = keyPath,
            });

            var ret = await proc.Process();
            Assert.Equal(order, ret);
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

#endif

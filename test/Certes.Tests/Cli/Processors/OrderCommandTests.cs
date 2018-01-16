#if NETCOREAPP1_0 || NETCOREAPP2_0

using System;
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

            options = Parse("order new");
            Assert.Equal(OrderAction.New, options.Action);
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

#if NETCOREAPP1_0 || NETCOREAPP2_0

using System;
using System.CommandLine;
using Certes.Cli.Options;
using Xunit;

namespace Certes.Cli.Processors
{
    public class AzureCommandTests
    {
        [Fact]
        public void CanParseCommnad()
        {
            var talentId = Guid.NewGuid();
            var subscriptionId = Guid.NewGuid();
            var orderUir = new Uri("http://acme.d/order/1");
            var host = "example-domain.com";

            var options = Parse("noop");
            Assert.Null(options);

            // deploy dns using service-principal
            options = Parse($"azure dns --user certes --pwd abcd1234 --talent {talentId} --subscription {subscriptionId} --order {orderUir} {host}");
            Assert.Equal(AzureAction.Dns, options.Action);
            Assert.Equal("certes", options.UserName);
            Assert.Equal("abcd1234", options.Password);
            Assert.Equal(talentId, options.Talent);
            Assert.Equal(subscriptionId, options.Subscription);
            Assert.Equal(orderUir, options.OrderUri);
            Assert.Equal(host, options.Value);
            Assert.Equal(AuzreCloudEnvironment.Global, options.CloudEnvironment);

        }

        private AzureOptions Parse(string cmd)
        {
            AzureOptions options = null;
            ArgumentSyntax.Parse(cmd.Split(' '), syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineCommand("noop");
                options = AzureCommand.TryParse(syntax);
            });

            return options;
        }
    }
}

#endif

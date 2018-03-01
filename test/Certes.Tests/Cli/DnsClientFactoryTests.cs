using System;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Xunit;

namespace Certes.Cli
{
    public class DnsClientFactoryTests
    {
        [Fact]
        public void CanCreateClient()
        {
            var factory = new DnsClientFactory();
            using (var client = factory.Create(
                new AzureCredentials(
                    new ServicePrincipalLoginInformation(),
                    Guid.NewGuid().ToString(),
                    AzureEnvironment.AzureGlobalCloud)))
            {
                Assert.NotNull(client);
            }
        }
    }
}

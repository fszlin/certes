using System;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Xunit;

namespace Certes.Cli
{
    public class AppServiceClientTests
    {
        [Fact]
        public void CanCreateClient()
        {
            var factory = new AppServiceClientFactory();
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

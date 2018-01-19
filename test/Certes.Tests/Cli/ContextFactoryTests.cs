using System;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Xunit;

namespace Certes.Cli
{
    [Collection(nameof(ContextFactory))]
    public class ContextFactoryTests
    {
        [Fact]
        public void CanGetSetProperties()
        {
            Func<Uri, IKey, IAcmeContext> cb = (Uri loc, IKey key) => null;

            ContextFactory.Create = cb;
            Assert.Equal(cb, ContextFactory.Create);
        }

        [Fact]
        public void CanCreateWebAppClinet()
        {
            ContextFactory.CreateAppServiceManagementClient = ContextFactory.DefaultCreateAppServiceManagementClient;
            Assert.Equal(ContextFactory.DefaultCreateAppServiceManagementClient, ContextFactory.CreateAppServiceManagementClient);
            
            var credentials = new AzureCredentials(new UserLoginInformation(), "talentId", AzureEnvironment.AzureGlobalCloud);
            using (var client = ContextFactory.CreateAppServiceManagementClient(credentials))
            {
                Assert.IsType<WebSiteManagementClient>(client);
            }
        }

        [Fact]
        public void CanCreateDnsClinet()
        {
            ContextFactory.CreateDnsManagementClient = ContextFactory.DefaultCreateDnsManagementClient;
            Assert.Equal(ContextFactory.DefaultCreateDnsManagementClient, ContextFactory.CreateDnsManagementClient);

            var credentials = new AzureCredentials(new UserLoginInformation(), "talentId", AzureEnvironment.AzureGlobalCloud);
            using (var client = ContextFactory.CreateDnsManagementClient(credentials))
            {
                Assert.IsType<DnsManagementClient>(client);
            }
        }
    }
}

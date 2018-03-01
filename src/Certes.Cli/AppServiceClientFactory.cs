using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Certes.Cli
{
    public class AppServiceClientFactory : IAppServiceClientFactory
    {
        public IWebSiteManagementClient Create(AzureCredentials credentials)
            => new WebSiteManagementClient(credentials);
    }
}

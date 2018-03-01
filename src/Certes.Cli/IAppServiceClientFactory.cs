using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Certes.Cli
{
    public interface IAppServiceClientFactory
    {
        IWebSiteManagementClient Create(AzureCredentials credentials);
    }
}

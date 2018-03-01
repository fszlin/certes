using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Certes.Cli
{
    internal interface IResourceClientFactory
    {
        IResourceManagementClient Create(AzureCredentials credentials);
    }
}

using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Certes.Cli
{
    internal class ResourceClientFactory : IResourceClientFactory
    {
        public IResourceManagementClient Create(AzureCredentials credentials)
            => new ResourceManagementClient(credentials);
    }
}

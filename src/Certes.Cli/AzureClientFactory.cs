using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Certes.Cli
{
    internal delegate T AzureClientFactory<T>(AzureCredentials credentials);
}

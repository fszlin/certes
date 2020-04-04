using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Certes.Cli
{
    internal delegate T AzureClientFactory<T>(RestClient restClient);
}

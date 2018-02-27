using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Certes.Cli
{
    internal interface IDnsClientFactory
    {
        IDnsManagementClient Create(AzureCredentials credentials);
    }
}

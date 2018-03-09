using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Certes.Cli.Azure
{
    public delegate IDnsManagementClient AzureDnsClientFactory(AzureCredentials credentials);
}

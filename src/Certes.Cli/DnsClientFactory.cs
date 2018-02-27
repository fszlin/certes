using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Certes.Cli
{
    public class DnsClientFactory : IDnsClientFactory
    {
        public IDnsManagementClient Create(AzureCredentials credentials)
            => new DnsManagementClient(credentials);
    }
}

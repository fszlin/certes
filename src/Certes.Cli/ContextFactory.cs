using System;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Rest;

namespace Certes.Cli
{
    internal static class ContextFactory
    {
        public static Func<Uri, IKey, IAcmeContext> DefaultCreate { get; } =
            (directoryUri, accountKey) => new Certes.AcmeContext(directoryUri, accountKey);

        public static Func<Uri, IKey, IAcmeContext> Create { get; set; } = DefaultCreate;

        public static Func<ServiceClientCredentials, IDnsManagementClient> CreateDnsManagementClient { get; set; } =
            credentials => new DnsManagementClient(credentials);
    }
}

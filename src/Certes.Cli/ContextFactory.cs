using System;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Rest;

namespace Certes.Cli
{
    internal static class ContextFactory
    {
        public static Func<Uri, IKey, IAcmeContext> DefaultCreate { get; } =
            (directoryUri, accountKey) => new Certes.AcmeContext(directoryUri, accountKey);

        public static Func<ServiceClientCredentials, IDnsManagementClient> DefaultCreateDnsManagementClient { get; } =
            credentials => new DnsManagementClient(credentials);

        public static Func<ServiceClientCredentials, IWebSiteManagementClient> DefaultCreateAppServiceManagementClient { get; } =
            credentials => new WebSiteManagementClient(credentials);

        public static Func<Uri, IKey, IAcmeContext> Create { get; set; } = DefaultCreate;

        public static Func<ServiceClientCredentials, IDnsManagementClient> CreateDnsManagementClient { get; set; } = DefaultCreateDnsManagementClient;

        public static Func<ServiceClientCredentials, IWebSiteManagementClient> CreateAppServiceManagementClient { get; set; } = DefaultCreateAppServiceManagementClient;
    }
}

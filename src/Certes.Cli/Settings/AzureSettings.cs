using System;
using Certes.Cli.Options;

namespace Certes.Cli.Settings
{
    internal class AzureSettings
    {
        public AzureCloudEnvironment Environment { get; set; }
        public Guid SubscriptionId { get; set; }
        public Guid Talent { get; set; }
        public string Secret { get; set; }
        public string ClientId { get; set; }
    }
}

using System;

namespace Certes.Cli.Settings
{
    internal class AzureSettings
    {
        public Guid SubscriptionId { get; set; }
        public Guid Talent { get; set; }
        public string Secret { get; set; }
        public string ClientId { get; set; }
    }
}

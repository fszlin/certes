using System;

namespace Certes.Cli.Options
{
    internal class AzureOptions : OptionsV2Base
    {
        public AzureAction Action;
        public AzureCloudEnvironment CloudEnvironment;
        public string UserName;
        public string Password;
        public Guid Talent;
        public Guid Subscription;
        public Uri OrderUri;
        public string ResourceGroup;
        public string Value;
    }
}

using System;

namespace Certes.Cli.Options
{
    internal class AzureOptions : OptionsV2Base
    {
        public AzureAction Action;
        public AuzreCloudEnvironment CloudEnvironment;
        public string UserName;
        public string Password;
        public Guid Talent;
        public Guid Subscription;
        public Uri OrderUri;
        public string Value;
    }
}

using Certes.Cli.Settings;

namespace Certes.Cli.Commands
{
    internal class AzureOptions : AzureSettings
    {
        public string ResourceGroup { get; set; }
    }
}

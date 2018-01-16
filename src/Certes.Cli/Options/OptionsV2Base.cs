using Certes.Acme;

namespace Certes.Cli.Options
{
    internal class OptionsV2Base : OptionsBase
    {
        public OptionsV2Base()
        {
            Path = "";
            Server = WellKnownServers.LetsEncryptStagingV2;
        }
    }
}

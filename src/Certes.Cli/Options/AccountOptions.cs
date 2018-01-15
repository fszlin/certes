using Certes.Acme;

namespace Certes.Cli.Options
{
    internal class AccountOptions : OptionsBase
    {
        public AccountAction Action;
        public string Email = "";
        public bool AgreeTos = false;

        public AccountOptions()
        {
            Path = "";
            Server = WellKnownServers.LetsEncryptStagingV2;
        }
    }
}

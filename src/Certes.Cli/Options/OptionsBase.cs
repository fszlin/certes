using System;
using Certes.Acme;

namespace Certes.Cli.Options
{
    internal class OptionsBase
    {
#if DEBUG
        public Uri Server = WellKnownServers.LetsEncryptStaging;
#else
        public Uri Server = WellKnownServers.LetsEncrypt;
#endif
        public string Path = "./data.json";
        public bool Force = false;
        public bool Verbose = false;
    }
}

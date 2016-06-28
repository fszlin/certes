using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Cli.Options
{
    internal class OptionsBase
    {
#if DEBUG
        public Uri Server = new Uri("https://acme-v01.api.letsencrypt.org/directory");
#else
        public Uri Server = new Uri("https://acme-v01.api.letsencrypt.org/directory");
#endif
        public string Path = "./data.json";
        public bool Force = false;
    }
}

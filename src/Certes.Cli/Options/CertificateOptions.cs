using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Cli.Options
{
    internal class CertificateOptions : OptionsBase
    {
        public string Name = "";
        public string DistinguishedName = "";
        public IReadOnlyList<string> Values = Array.Empty<string>();
        public string ValuesFile = "";
        public string ExportPfx = "";
        public string ExportKey = "";
        public string ExportCer = "";
        public bool RevokeCer;
        public string Password = "";
        public bool NoChain = false;
    }
}

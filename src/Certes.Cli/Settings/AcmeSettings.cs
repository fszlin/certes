using System;

namespace Certes.Cli.Settings
{
    internal class AcmeSettings
    {
        public Uri ServerUri { get; set; }
        public string AccountKey { get; set; }
        public byte[] Key { get; set; }
    }
}

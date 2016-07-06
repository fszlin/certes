using System;

namespace Certes.Acme
{
    public class WellKnownServers
    {
        public static Uri LetsEncrypt { get; } = new Uri("https://acme-v01.api.letsencrypt.org/directory");
        public static Uri LetsEncryptStaging { get; } = new Uri("https://acme-staging.api.letsencrypt.org/directory");
    }
}

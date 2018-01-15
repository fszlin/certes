using Certes.Acme;
using System;
using System.IO;
using System.Threading.Tasks;

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
    }

    internal static class OptionsExtensions
    {
        public static async Task<IKey> LoadKey(this OptionsBase options)
        {
            var path = string.IsNullOrWhiteSpace(options.Path) ? GetDefaultKeyPath() : options.Path;
            if (!File.Exists(path))
            {
                return null;
            }

            var pem = await FileUtil.ReadAllText(path);
            return KeyFactory.FromPem(pem);
        }

        private static string GetDefaultKeyPath()
        {
            var homePath = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            if (!Directory.Exists(homePath))
            {
                homePath = Environment.GetEnvironmentVariable("HOME");
            }

            return Path.Combine(homePath, ".certes", "account.pem");
        }
    }
}

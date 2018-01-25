using System;
using System.IO;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Cli.Options;
using Certes.Cli.Settings;
using Xunit;

namespace Certes.Cli
{
    [Collection(nameof(ContextFactory))]
    public class UserSettingsTests
    {
        [Fact]
        public async Task CanLoadKeyFromPath()
        {
            File.WriteAllText("./Data/key-es256.pem", Helper.GetTestKey(KeyAlgorithm.ES256));

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
                Path = "./Data/key-es256.pem",
            };

            var key = await UserSettings.GetAccountKey(options, false);

            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task CanLoadKeyFromUnixEvn()
        {
            var fullPath = Path.GetFullPath("./");
            Environment.SetEnvironmentVariable("HOMEDRIVE", "");
            Environment.SetEnvironmentVariable("HOMEPATH", "");
            Environment.SetEnvironmentVariable("HOME", fullPath);

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            await UserSettings.SetAcmeSettings(new AcmeSettings
            {
                ServerUri = WellKnownServers.LetsEncryptStagingV2,
                AccountKey = Helper.GetTestKey(KeyAlgorithm.ES256),
            }, options);

            var key = await UserSettings.GetAccountKey(options, false);

            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task CanLoadKeyFromWinEvn()
        {
            var fullPath = Path.GetFullPath("./");
            var drive = Path.GetPathRoot(fullPath);
            Environment.SetEnvironmentVariable("HOME", "");
            Environment.SetEnvironmentVariable("HOMEDRIVE", drive);
            Environment.SetEnvironmentVariable("HOMEPATH", fullPath.Substring(drive.Length));

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            await UserSettings.SetAcmeSettings(new AcmeSettings
            {
                ServerUri = WellKnownServers.LetsEncryptStagingV2,
                AccountKey = Helper.GetTestKey(KeyAlgorithm.ES256),
            }, options);

            var key = await UserSettings.GetAccountKey(options, false);

            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task NullWhenKeyNotExist()
        {
            if (Directory.Exists("./.certes/"))
            {
                Directory.Delete("./.certes/", true);
            }

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            var key = await UserSettings.GetAccountKey(options, false);
            Assert.Null(key);
        }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Cli.Options;
using Certes.Cli.Settings;
using Xunit;

using static Certes.Helper;

namespace Certes.Cli
{
    [Collection(nameof(ContextFactory))]
    public class UserSettingsTests
    {
        [Fact]
        public async Task CanLoadKeyFromPath()
        {
            var fullPath = Path.GetFullPath($"./{nameof(CanLoadKeyFromPath)}");
            SetHomePath(fullPath);

            File.WriteAllText("./Data/key-es256.pem", Helper.GetTestKey(KeyAlgorithm.ES256));

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
                Path = "./Data/key-es256.pem",
            };

            var userSettings = new UserSettings();
            var key = await userSettings.GetAccountKey(options, false);

            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task CanLoadKeyFromUnixEvn()
        {
            var fullPath = Path.GetFullPath($"./{nameof(CanLoadKeyFromUnixEvn)}");
            SetHomePath(fullPath, false);

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            var userSettings = new UserSettings();
            await userSettings.SetAcmeSettings(new AcmeSettings
            {
                ServerUri = WellKnownServers.LetsEncryptStagingV2,
                AccountKey = Helper.GetTestKey(KeyAlgorithm.ES256),
            }, options);

            var key = await userSettings.GetAccountKey(options, false);

            Assert.Equal(Path.Combine(fullPath, ".certes", "certes.json"), userSettings.SettingsPath.Value);
            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task CanLoadKeyFromWinEvn()
        {
            var fullPath = Path.GetFullPath($"./{nameof(CanLoadKeyFromWinEvn)}");
            SetHomePath(fullPath);

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            var userSettings = new UserSettings();
            await userSettings.SetAcmeSettings(new AcmeSettings
            {
                ServerUri = WellKnownServers.LetsEncryptStagingV2,
                AccountKey = Helper.GetTestKey(KeyAlgorithm.ES256),
            }, options);

            var key = await userSettings.GetAccountKey(options, false);

            Assert.Equal(Path.Combine(fullPath, ".certes", "certes.json"), userSettings.SettingsPath.Value);
            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task NullWhenKeyNotExist()
        {
            var fullPath = Path.GetFullPath($"./{nameof(NullWhenKeyNotExist)}");
            SetHomePath(fullPath);

            var userSettings = new UserSettings();
            if (Directory.Exists(userSettings.SettingsPath.Value))
            {
                Directory.Delete(userSettings.SettingsPath.Value, true);
            }

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            var key = await userSettings.GetAccountKey(options, false);
            Assert.Null(key);
        }
    }
}

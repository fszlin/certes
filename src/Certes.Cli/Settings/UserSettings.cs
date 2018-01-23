using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Options;
using Certes.Json;
using Newtonsoft.Json;

namespace Certes.Cli.Settings
{
    internal class UserSettings
    {
        public IList<AcmeSettings> Servers { get; set; }
        public AzureSettings Azure { get; set; }

        public static Lazy<string> SettingsPath = new Lazy<string>(
            () =>
            {
                var homePath = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
                if (!Directory.Exists(homePath))
                {
                    homePath = Environment.GetEnvironmentVariable("HOME");
                }

                return Path.Combine(homePath, ".certes", "certes.json");
            });

        public static async Task SetAcmeSettings(AcmeSettings acme, OptionsBase options)
        {
            if (string.IsNullOrWhiteSpace(options.Path))
            {
                var settings = await LoadUserSettings();
                var currentAcme = settings.Servers?
                    .FirstOrDefault(s => s.ServerUri == acme.ServerUri);

                if (currentAcme == null)
                {
                    if (settings.Servers == null)
                    {
                        settings.Servers = new List<AcmeSettings>();
                    }

                    settings.Servers.Add(currentAcme = acme);
                }
                else
                {
                    currentAcme.AccountKey = acme.AccountKey;
                }

                var json = JsonConvert.SerializeObject(settings, JsonUtil.CreateSettings());
                await FileUtil.WriteAllTexts(SettingsPath.Value, json);
            }
            else if (!string.IsNullOrWhiteSpace(acme.AccountKey))
            {
                await FileUtil.WriteAllTexts(options.Path, acme.AccountKey);
            }
        }

        public static async Task<AzureSettings> GetAzureSettings(AzureOptions options)
        {
            var settings = await LoadUserSettings();

            var azure = settings.Azure ?? new AzureSettings();

            azure.Environment = options.CloudEnvironment == AzureCloudEnvironment.Default ?
                AzureCloudEnvironment.Global : options.CloudEnvironment;

            if (options.Talent != null)
            {
                azure.Talent = options.Talent;
            }

            if (!string.IsNullOrWhiteSpace(options.UserName))
            {
                azure.ClientId = options.UserName;
            }

            if (!string.IsNullOrWhiteSpace(options.Password))
            {
                azure.Secret = options.Password;
            }

            if (options.Subscription != null)
            {
                azure.SubscriptionId = options.Subscription;
            }

            return azure;
        }

        public static async Task<AcmeSettings> GetAcmeSettings(OptionsBase options)
        {
            var settings = await LoadUserSettings();

            var serverUri = options.Server;
            var acme = settings.Servers?
                .FirstOrDefault(s => s.ServerUri == serverUri);

            if (acme == null)
            {
                acme = new AcmeSettings { ServerUri = serverUri };
            }

            if (!string.IsNullOrWhiteSpace(options.Path))
            {
                acme.AccountKey = await FileUtil.ReadAllText(options.Path);
            }

            return acme;
        }

        public static async Task<IKey> GetAccountKey(OptionsBase options, bool accountKeyRequired = false)
        {
            var settings = await GetAcmeSettings(options);

            if (string.IsNullOrWhiteSpace(settings.AccountKey))
            {
                if (accountKeyRequired)
                {
                    throw new Exception("No account key is available.");
                }

                return null;
            }

            return KeyFactory.FromPem(settings.AccountKey);
        }

        private static async Task<UserSettings> LoadUserSettings()
        {
            UserSettings settings;
            if (File.Exists(SettingsPath.Value))
            {
                var json = await FileUtil.ReadAllText(SettingsPath.Value);
                settings = JsonConvert.DeserializeObject<UserSettings>(json, JsonUtil.CreateSettings());
            }
            else
            {
                settings = new UserSettings();
            }

            return settings;
        }
    }

}

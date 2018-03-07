using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Json;
using Newtonsoft.Json;

namespace Certes.Cli.Settings
{
    internal class UserSettings : IUserSettings
    {
        internal class Model
        {
            public Uri DefaultServer { get; set; }
            public IList<AcmeSettings> Servers { get; set; }
            public AzureSettings Azure { get; set; }
        }

        private readonly IFileUtil fileUtil;
        private readonly IEnvironmentVariables environment;
        private readonly Lazy<string> settingsFilepath;

        public UserSettings(IFileUtil fileUtil, IEnvironmentVariables environment)
        {
            this.fileUtil = fileUtil;
            this.environment = environment;
            settingsFilepath = new Lazy<string>(ReadSettingsFilepath);
        }

        public async Task SetDefaultServer(Uri serverUri)
        {
            var settings = await LoadUserSettings();

            settings.DefaultServer = serverUri;
            var json = JsonConvert.SerializeObject(settings, JsonUtil.CreateSettings());
            await fileUtil.WriteAllText(settingsFilepath.Value, json);
        }

        public async Task<Uri> GetDefaultServer()
        {
            var settings = await LoadUserSettings();

            return settings.DefaultServer ?? WellKnownServers.LetsEncryptV2;
        }

        public async Task SetAccountKey(Uri serverUri, IKey key)
        {
            var settings = await LoadUserSettings();
            if (settings.Servers == null)
            {
                settings.Servers = new AcmeSettings[0];
            }

            var servers = settings.Servers.ToList();
            var serverSetting = servers.FirstOrDefault(s => s.ServerUri == serverUri);
            if (serverSetting == null)
            {
                servers.Add(serverSetting = new AcmeSettings { ServerUri = serverUri });
            }

            serverSetting.Key = key.ToDer();
            settings.Servers = servers;
            var json = JsonConvert.SerializeObject(settings, JsonUtil.CreateSettings());
            await fileUtil.WriteAllText(settingsFilepath.Value, json);
        }

        public async Task<IKey> GetAccountKey(Uri serverUri)
        {
            // env settings overwrites user settings
            var envKey = environment.GetVar("CERTES_ACME_ACCOUNT_KEY");
            if (envKey != null)
            {
                return KeyFactory.FromDer(Convert.FromBase64String(envKey));
            }

            var settings = await LoadUserSettings();
            var serverSetting = settings.Servers?.FirstOrDefault(s => s.ServerUri == serverUri);
            var der = serverSetting?.Key;
            return der == null ? null : KeyFactory.FromDer(der);
        }

        public async Task<AzureSettings> GetAzureSettings()
        {
            var settings = await LoadUserSettings();
            var azSettings = settings.Azure ?? new AzureSettings();

            PopulateSettings(azSettings);

            return azSettings;
        }

        public async Task SetAzureSettings(AzureSettings azSettings)
        {
            var settings = await LoadUserSettings();

            settings.Azure = azSettings;
            var json = JsonConvert.SerializeObject(settings, JsonUtil.CreateSettings());
            await fileUtil.WriteAllText(settingsFilepath.Value, json);
        }

        private async Task<Model> LoadUserSettings()
        {
            var json = await fileUtil.ReadAllText(settingsFilepath.Value);
            return json == null ?
                new Model() :
                JsonConvert.DeserializeObject<Model>(json, JsonUtil.CreateSettings());
        }

        private string ReadSettingsFilepath()
        {
            var homePath = environment.GetVar("HOMEDRIVE") + environment.GetVar("HOMEPATH");
            if (string.IsNullOrWhiteSpace(homePath))
            {
                homePath = environment.GetVar("HOME");
            }

            return Path.Combine(homePath, ".certes", "certes.json");
        }

        private void PopulateSettings(AzureSettings settings)
        {
            var envSubscriptionId = environment.GetVar("CERTES_AZURE_SUBSCRIPTION_ID");
            if (!string.IsNullOrWhiteSpace(envSubscriptionId))
            {
                settings.SubscriptionId = envSubscriptionId;
            }

            var envTalentId = environment.GetVar("CERTES_AZURE_TALENT_ID");
            if (!string.IsNullOrWhiteSpace(envTalentId))
            {
                settings.TalentId = envTalentId;
            }

            var envClientId = environment.GetVar("CERTES_AZURE_CLIENT_ID");
            if (!string.IsNullOrWhiteSpace(envClientId))
            {
                settings.ClientId = envClientId;
            }

            var envClientSecret = environment.GetVar("CERTES_AZURE_CLIENT_SECRET");
            if (!string.IsNullOrWhiteSpace(envClientSecret))
            {
                settings.ClientSecret = envClientSecret;
            }
        }
    }
}

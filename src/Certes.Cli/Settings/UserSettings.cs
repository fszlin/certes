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

        private readonly static Func<string> SettingsPathFactory = () =>
        {
            var homePath = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
            if (!Directory.Exists(homePath))
            {
                homePath = Environment.GetEnvironmentVariable("HOME");
            }

            return Path.Combine(homePath, ".certes", "certes.json");
        };

        private readonly IFileUtil fileUtil;

        public Lazy<string> SettingsFile { get; set; } = new Lazy<string>(SettingsPathFactory);

        public UserSettings(IFileUtil fileUtil)
        {
            this.fileUtil = fileUtil;
        }

        public async Task SetDefaultServer(Uri serverUri)
        {
            var settings = await LoadUserSettings();

            settings.DefaultServer = serverUri;
            var json = JsonConvert.SerializeObject(settings, JsonUtil.CreateSettings());
            await fileUtil.WriteAllText(SettingsFile.Value, json);
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
            await fileUtil.WriteAllText(SettingsFile.Value, json);
        }

        public async Task<IKey> GetAccountKey(Uri serverUri)
        {
            var settings = await LoadUserSettings();
            var serverSetting = settings.Servers?.FirstOrDefault(s => s.ServerUri == serverUri);
            var der = serverSetting?.Key;
            return der == null ? null : KeyFactory.FromDer(der);
        }

        public async Task<AzureSettings> GetAzureSettings()
        {
            var settings = await LoadUserSettings();
            return settings.Azure ?? new AzureSettings();
        }

        private async Task<Model> LoadUserSettings()
        {
            var json = await fileUtil.ReadAllText(SettingsFile.Value);
            return json == null ?
                new Model() :
                JsonConvert.DeserializeObject<Model>(json, JsonUtil.CreateSettings());
        }
    }

}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Certes.Acme;
using Certes.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Certes.Cli.Settings
{
    internal class UserSettings : IUserSettings
    {
        internal class Model
        {
            public Uri DefaultServer { get; set; }
            public IList<AcmeSettings> Servers { get; set; }
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

        public async Task SetDefaultServer(Uri serverUri, CancellationToken cancellationToken = default)
        {
            var settings = await LoadUserSettings(cancellationToken);

            settings.DefaultServer = serverUri;
            var json = JsonSerializer.Serialize(settings, JsonUtil.CreateSettings());
            await fileUtil.WriteAllText(settingsFilepath.Value, json, cancellationToken);
        }

        public async Task<Uri> GetDefaultServer(CancellationToken cancellationToken = default)
        {
            var settings = await LoadUserSettings(cancellationToken);

            return settings.DefaultServer ?? WellKnownServers.LetsEncryptV2;
        }

        public async Task SetAccountKey(Uri serverUri, IKey key, CancellationToken cancellationToken = default)
        {
            var settings = await LoadUserSettings(cancellationToken);
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
            var json = JsonSerializer.Serialize(settings, JsonUtil.CreateSettings());
            await fileUtil.WriteAllText(settingsFilepath.Value, json, cancellationToken);
        }

        public async Task<IKey> GetAccountKey(Uri serverUri, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // env settings overwrites user settings
            var envKey = environment.GetVar("CERTES_ACME_ACCOUNT_KEY");
            if (envKey != null)
            {
                return KeyFactory.FromDer(Convert.FromBase64String(envKey));
            }

            var settings = await LoadUserSettings(cancellationToken);
            var serverSetting = settings.Servers?.FirstOrDefault(s => s.ServerUri == serverUri);
            var der = serverSetting?.Key;
            return der == null ? null : KeyFactory.FromDer(der);
        }

        private async Task<Model> LoadUserSettings(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var json = await fileUtil.ReadAllText(settingsFilepath.Value, cancellationToken);
            return json == null ?
                new Model() :
                JsonSerializer.Deserialize<Model>(json, JsonUtil.CreateSettings());
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
    }
}

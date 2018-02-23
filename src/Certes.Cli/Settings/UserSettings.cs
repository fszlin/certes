﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Cli.Options;
using Certes.Json;
using Newtonsoft.Json;

namespace Certes.Cli.Settings
{
    internal class UserSettings : IUserSettings
    {
        private class Model
        {
            public Uri DefaultServer { get; set; }
            public IList<AcmeSettings> Servers { get; set; }
            public AzureSettings Azure { get; set; }
        }

        private readonly IFileUtil fileUtil;

        public UserSettings(IFileUtil fileUtil)
        {
            this.fileUtil = fileUtil;
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

        public Lazy<string> SettingsFile { get; set; } = new Lazy<string>(SettingsPathFactory);

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

        public async Task SetAcmeSettings(AcmeSettings acme, OptionsBase options)
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
                await fileUtil.WriteAllText(SettingsFile.Value, json);
            }
            else if (!string.IsNullOrWhiteSpace(acme.AccountKey))
            {
                await fileUtil.WriteAllText(options.Path, acme.AccountKey);
            }
        }

        public async Task<AzureSettings> GetAzureSettings(AzureOptions options)
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

        public async Task<AcmeSettings> GetAcmeSettings(OptionsBase options)
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
                acme.AccountKey = await fileUtil.ReadAllText(options.Path);
            }

            return acme;
        }

        public async Task<IKey> GetAccountKey(OptionsBase options, bool accountKeyRequired = false)
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

        private async Task<Model> LoadUserSettings()
        {
            Model settings;
            if (File.Exists(SettingsFile.Value))
            {
                var json = await fileUtil.ReadAllText(SettingsFile.Value);
                settings = JsonConvert.DeserializeObject<Model>(json, JsonUtil.CreateSettings());
            }
            else
            {
                settings = new Model();
            }

            return settings;
        }

        public async Task<IKey> GetAccountKey(Uri serverUri)
        {
            var settings = await LoadUserSettings();
            var serverSetting = settings.Servers?.FirstOrDefault(s => s.ServerUri == serverUri);
            var der = serverSetting?.Key;
            return der == null ? KeyFactory.FromDer(der) : null;
        }
    }

}

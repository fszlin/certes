using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Commands;
using Certes.Cli.Settings;
using Certes.Json;
using Certes.Pkcs;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using NLog;
using Spectre.Console.Cli;

namespace Certes.Cli
{
    /// <summary>
    /// Spectre.Console.Cli-based CLI implementation.
    /// Spectre handles command parsing and dispatch.
    /// </summary>
    internal class CliCoreSpectre
    {
        private readonly ILogger consoleLogger = LogManager.GetLogger(nameof(CliCoreSpectre));
        private static readonly JsonSerializerOptions jsonSerializerSettings = JsonUtil.CreateSettings();
        private readonly IEnumerable<ICliCommand> commands;
        private readonly IUserSettings userSettings;
        private readonly AcmeContextFactory contextFactory;
        private readonly IFileUtil fileUtil;
        private readonly IEnvironmentVariables environmentVariables;
        private readonly AzureClientFactory<IResourceManagementClient> resourceClientFactory;
        private readonly AzureClientFactory<IDnsManagementClient> dnsClientFactory;
        private readonly AzureClientFactory<IWebSiteManagementClient> webSiteClientFactory;

        public CliCoreSpectre(
            IEnumerable<ICliCommand> commands,
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil,
            IEnvironmentVariables environmentVariables,
            AzureClientFactory<IResourceManagementClient> resourceClientFactory,
            AzureClientFactory<IDnsManagementClient> dnsClientFactory,
            AzureClientFactory<IWebSiteManagementClient> webSiteClientFactory)
        {
            this.commands = commands;
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
            this.fileUtil = fileUtil;
            this.environmentVariables = environmentVariables;
            this.resourceClientFactory = resourceClientFactory;
            this.dnsClientFactory = dnsClientFactory;
            this.webSiteClientFactory = webSiteClientFactory;
        }

        /// <summary>
        /// Runs the CLI using Spectre for parsing and dispatch.
        /// </summary>
        public async Task<bool> Run(string[] args)
        {
            try
            {
                var app = new CommandApp();
                app.Configure(config =>
                {
                    config.SetApplicationName("certes");

                    config.Settings.StrictParsing = false;
                    config.Settings.ConvertFlagsToRemainingArguments = true;

                    ConfigureServerBranch(config);
                    ConfigureAccountBranch(config);
                    ConfigureOrderBranch(config);
                    ConfigureCertificateBranch(config);
                    ConfigureAzureBranch(config);
                });

                var result = app.Run(args);
                return result == 0;
            }
            catch (Exception ex)
            {
                consoleLogger.Error(ex.Message);
                consoleLogger.Debug(ex);
                return false;
            }
        }

        private void ConfigureServerBranch(IConfigurator config)
        {
            config.AddBranch(CommandGroup.Server.Command, branch =>
            {
                branch.AddAsyncDelegate<ServerSetSettings>("set", async (_, settings) =>
                {
                    var ctx = contextFactory.Invoke(settings.NewServer, null);
                    consoleLogger.Debug("Loading directory from '{0}'", settings.NewServer);
                    var directory = await ctx.GetDirectory();
                    await userSettings.SetDefaultServer(settings.NewServer);

                    WriteJson(new
                    {
                        location = settings.NewServer,
                        resource = directory,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandServerSet);

                branch.AddAsyncDelegate<ServerShowSettings>("show", async (_, settings) =>
                {
                    var serverUri = settings.Server ?? await userSettings.GetDefaultServer();
                    var ctx = contextFactory.Invoke(serverUri, null);
                    consoleLogger.Debug("Loading directory from '{0}'", serverUri);
                    var directory = await ctx.GetDirectory();

                    WriteJson(new
                    {
                        location = serverUri,
                        resource = directory,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandServerShow);
            });
        }

        private void ConfigureAccountBranch(IConfigurator config)
        {
            config.AddBranch(CommandGroup.Account.Command, branch =>
            {
                branch.AddAsyncDelegate<AccountNewSettings>("new", async (_, settings) =>
                {
                    var account = await ReadAccountKey(settings.Server, settings.KeyPath);
                    var key = account.Key ?? KeyFactory.NewKey(KeyAlgorithm.ES256);

                    consoleLogger.Debug("Creating new account on '{0}'.", account.Server);
                    var acme = contextFactory.Invoke(account.Server, key);
                    var acctCtx = await acme.NewAccount(settings.Email, true);

                    if (!string.IsNullOrWhiteSpace(settings.OutPath))
                    {
                        consoleLogger.Debug("Saving new account key to '{0}'.", settings.OutPath);
                        await System.IO.File.WriteAllTextAsync(settings.OutPath, key.ToPem());
                    }
                    else
                    {
                        consoleLogger.Debug("Saving new account key to user settings.");
                        await userSettings.SetAccountKey(account.Server, key);
                    }

                    WriteJson(new
                    {
                        location = acctCtx.Location,
                        resource = await acctCtx.Resource(),
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandAccountNew);

                branch.AddAsyncDelegate<AccountSetSettings>("set", async (_, settings) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, settings.KeyPath, required: true);

                    consoleLogger.Debug("Setting account for '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var acctCtx = await acme.Account();
                    await userSettings.SetAccountKey(serverUri, key);

                    WriteJson(new
                    {
                        location = acctCtx.Location,
                        resource = await acctCtx.Resource(),
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandAccountSet);

                branch.AddAsyncDelegate<AccountShowSettings>("show", async (_, settings) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, settings.KeyPath, fallbackToSettings: true, required: true);

                    consoleLogger.Debug("Loading account from '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var acctCtx = await acme.Account();

                    WriteJson(new
                    {
                        location = acctCtx.Location,
                        resource = await acctCtx.Resource(),
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandAccountShow);

                branch.AddAsyncDelegate<AccountUpdateSettings>("update", async (_, settings) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, settings.KeyPath, fallbackToSettings: true, required: true);

                    consoleLogger.Debug("Updating account on '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var acctCtx = await acme.Account();
                    var account = await acctCtx.Update(new[] { $"mailto://{settings.Email}" }, true);

                    WriteJson(new
                    {
                        location = acctCtx.Location,
                        resource = account,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandAccountUpdate);
            });
        }

        private async Task<(Uri Server, IKey Key)> ReadAccountKey(
            Uri server,
            string keyPath = null,
            bool fallbackToSettings = false,
            bool required = false)
        {
            var serverUri = server ?? await userSettings.GetDefaultServer();

            if (!string.IsNullOrWhiteSpace(keyPath))
            {
                consoleLogger.Debug("Load account key form '{0}'.", keyPath);
                var pem = await fileUtil.ReadAllText(keyPath);
                return (serverUri, KeyFactory.FromPem(pem));
            }

            var key = fallbackToSettings
                ? await userSettings.GetAccountKey(serverUri)
                : null;

            if (required && key == null)
            {
                throw new CertesCliException(string.Format(Strings.ErrorNoAccountKey, serverUri));
            }

            return (serverUri, key);
        }

        private void ConfigureOrderBranch(IConfigurator config)
        {
            config.AddBranch(CommandGroup.Order.Command, branch =>
            {
                branch.AddAsyncDelegate<OrderNewSettings>("new", async (_, settings) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, settings.KeyPath, fallbackToSettings: true);
                    consoleLogger.Debug("Creating order from '{0}'.", serverUri);

                    var acme = contextFactory.Invoke(serverUri, key);
                    var orderCtx = await acme.NewOrder(settings.Domains);

                    WriteJson(new
                    {
                        location = orderCtx.Location,
                        resource = await orderCtx.Resource(),
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandOrderNew);

                branch.AddAsyncDelegate<OrderListSettings>("list", async (_, settings) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, settings.KeyPath, fallbackToSettings: true);
                    consoleLogger.Debug("Loading orders from '{0}'.", serverUri);

                    var acme = contextFactory.Invoke(serverUri, key);
                    var acctCtx = await acme.Account();
                    var orderListCtx = await acctCtx.Orders();
                    var orderList = new List<object>();

                    foreach (var orderCtx in await orderListCtx.Orders())
                    {
                        orderList.Add(new
                        {
                            location = orderCtx.Location,
                            resource = await orderCtx.Resource(),
                        });
                    }

                    WriteJson(orderList);
                    return 0;
                }).WithDescription(Strings.HelpCommandOrderList);

                branch.AddAsyncDelegate<OrderShowSettings>("show", async (_, settings) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, settings.KeyPath, fallbackToSettings: true);
                    consoleLogger.Debug("Loading order from '{0}'.", serverUri);

                    var acme = contextFactory.Invoke(serverUri, key);
                    var orderCtx = acme.Order(settings.OrderId);

                    WriteJson(new
                    {
                        location = orderCtx.Location,
                        resource = await orderCtx.Resource(),
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandOrderShow);

                branch.AddAsyncDelegate<OrderAuthzSettings>("authz", async (_, settings) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, settings.KeyPath, fallbackToSettings: true);
                    var type =
                        string.Equals(settings.ChallengeType, "dns", StringComparison.OrdinalIgnoreCase) ? ChallengeTypes.Dns01 :
                        string.Equals(settings.ChallengeType, "http", StringComparison.OrdinalIgnoreCase) ? ChallengeTypes.Http01 :
                        throw new CertesCliException(string.Format(Strings.ErrorInvalidChallengeType, settings.ChallengeType));

                    consoleLogger.Debug("Loading authz from '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var orderCtx = acme.Order(settings.OrderId);
                    var authzCtx = await orderCtx.Authorization(settings.Domain)
                        ?? throw new CertesCliException(string.Format(Strings.ErrorIdentifierNotAvailable, settings.Domain));
                    var challengeCtx = await authzCtx.Challenge(type)
                        ?? throw new CertesCliException(string.Format(Strings.ErrorChallengeNotAvailable, type));

                    var challenge = await challengeCtx.Resource();
                    if (string.Equals(type, ChallengeTypes.Dns01, StringComparison.OrdinalIgnoreCase))
                    {
                        WriteJson(new
                        {
                            location = challengeCtx.Location,
                            dnsTxt = key.DnsTxt(challenge.Token),
                            resource = challenge,
                        });
                    }
                    else
                    {
                        WriteJson(new
                        {
                            location = challengeCtx.Location,
                            challengeFile = $".well-known/acme-challenge/{challenge.Token}",
                            challengeTxt = $"{challenge.Token}.{key.Thumbprint()}",
                            resource = challenge,
                            keyAuthz = challengeCtx.KeyAuthz,
                        });
                    }

                    return 0;
                }).WithDescription(Strings.HelpCommandOrderAuthz);

                branch.AddAsyncDelegate<OrderValidateSettings>("validate", async (_, settings) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, settings.KeyPath, fallbackToSettings: true);
                    var type =
                        string.Equals(settings.ChallengeType, "dns", StringComparison.OrdinalIgnoreCase) ? ChallengeTypes.Dns01 :
                        string.Equals(settings.ChallengeType, "http", StringComparison.OrdinalIgnoreCase) ? ChallengeTypes.Http01 :
                        throw new CertesCliException(string.Format(Strings.ErrorInvalidChallengeType, settings.ChallengeType));

                    consoleLogger.Debug("Validating authz on '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var orderCtx = acme.Order(settings.OrderId);
                    var authzCtx = await orderCtx.Authorization(settings.Domain)
                        ?? throw new CertesCliException(string.Format(Strings.ErrorIdentifierNotAvailable, settings.Domain));
                    var challengeCtx = await authzCtx.Challenge(type)
                        ?? throw new CertesCliException(string.Format(Strings.ErrorChallengeNotAvailable, settings.ChallengeType));

                    consoleLogger.Debug("Validating challenge '{0}'.", challengeCtx.Location);
                    var challenge = await challengeCtx.Validate();

                    WriteJson(new
                    {
                        location = challengeCtx.Location,
                        resource = challenge,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandOrderValidate);

                branch.AddAsyncDelegate<OrderFinalizeSettings>("finalize", async (_, settings) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, settings.KeyPath, fallbackToSettings: true);
                    var providedKey = await CommandBase.ReadKey(settings.PrivateKey, "CERTES_CERT_KEY", fileUtil, environmentVariables);
                    var certificateKey = providedKey ?? KeyFactory.NewKey(settings.KeyAlgorithm);

                    consoleLogger.Debug("Finalizing order from '{0}'.", serverUri);
                    var acme = contextFactory.Invoke(serverUri, key);
                    var orderCtx = acme.Order(settings.OrderId);
                    var csr = await orderCtx.CreateCsr(certificateKey);
                    if (!string.IsNullOrWhiteSpace(settings.Dn))
                    {
                        csr.AddName(settings.Dn);
                    }

                    var order = await orderCtx.Finalize(csr.Generate());

                    if (string.IsNullOrWhiteSpace(settings.OutPath) && providedKey == null)
                    {
                        WriteJson(new
                        {
                            location = orderCtx.Location,
                            privateKey = certificateKey.ToDer(),
                            resource = order,
                        });
                    }
                    else
                    {
                        if (providedKey == null)
                        {
                            await fileUtil.WriteAllText(settings.OutPath, certificateKey.ToPem());
                        }

                        WriteJson(new
                        {
                            location = orderCtx.Location,
                            resource = order,
                        });
                    }

                    return 0;
                }).WithDescription(Strings.HelpCommandOrderFinalize);
            });
        }

        private void ConfigureCertificateBranch(IConfigurator config)
        {
            config.AddBranch(CommandGroup.Certificate.Command, branch =>
            {
                branch.AddAsyncDelegate<CertificatePemSettings>("pem", async (_, settings) =>
                {
                    var (location, cert) = await DownloadCertificate(settings.OrderId, settings.PreferredChain, settings.Server, settings.KeyPath);

                    if (string.IsNullOrWhiteSpace(settings.OutPath))
                    {
                        WriteJson(new
                        {
                            location,
                            resource = new
                            {
                                certificate = cert.Certificate.ToDer(),
                                issuers = cert.Issuers.Select(i => i.ToDer()).ToArray(),
                            },
                        });
                    }
                    else
                    {
                        consoleLogger.Debug("Saving certificate to '{0}'.", settings.OutPath);
                        await fileUtil.WriteAllText(settings.OutPath, cert.ToPem());

                        WriteJson(new
                        {
                            location,
                        });
                    }

                    return 0;
                }).WithDescription(Strings.HelpCommandCertificatePem);

                branch.AddAsyncDelegate<CertificatePfxSettings>("pfx", async (_, settings) =>
                {
                    var (location, cert) = await DownloadCertificate(settings.OrderId, settings.PreferredChain, settings.Server, settings.KeyPath);
                    var privKey = await CommandBase.ReadKey(settings.PrivateKey, "CERTES_CERT_KEY", fileUtil, environmentVariables);
                    if (privKey == null)
                    {
                        throw new CertesCliException(Strings.ErrorNoPrivateKey);
                    }

                    var pfxName = string.Format(CultureInfo.InvariantCulture, "[certes] {0:yyyyMMddhhmmss}", DateTime.UtcNow);
                    if (!string.IsNullOrWhiteSpace(settings.FriendlyName))
                    {
                        pfxName = string.Concat(settings.FriendlyName, " ", pfxName);
                    }

                    var pfxBuilder = cert.ToPfx(privKey);
                    if (!string.IsNullOrWhiteSpace(settings.Issuer))
                    {
                        var issuerPem = await fileUtil.ReadAllText(settings.Issuer);
                        pfxBuilder.AddIssuers(Encoding.UTF8.GetBytes(issuerPem));
                    }

                    var pfx = pfxBuilder.Build(pfxName, settings.Password);
                    if (string.IsNullOrWhiteSpace(settings.OutPath))
                    {
                        WriteJson(new
                        {
                            location,
                            pfx,
                        });
                    }
                    else
                    {
                        consoleLogger.Debug("Saving certificate to '{0}'.", settings.OutPath);
                        await fileUtil.WriteAllBytes(settings.OutPath, pfx);

                        WriteJson(new
                        {
                            location,
                        });
                    }

                    return 0;
                }).WithDescription(Strings.HelpCommandCertificatePem);
            });
        }

        private void ConfigureAzureBranch(IConfigurator config)
        {
            config.AddBranch(CommandGroup.Azure.Command, branch =>
            {
                branch.AddAsyncDelegate<AzureSetSettings>("set", async (_, settings) =>
                {
                    var loginInfo = new ServicePrincipalLoginInformation
                    {
                        ClientId = settings.ClientId,
                        ClientSecret = settings.ClientSecret,
                    };

                    var credentials = new AzureCredentials(loginInfo, settings.TenantId, AzureEnvironment.AzureGlobalCloud)
                        .WithDefaultSubscription(settings.SubscriptionId);

                    var restClient = RestClient.Configure()
                        .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                        .WithCredentials(credentials)
                        .Build();

                    var resourceGroups = await LoadResourceGroups(restClient);
                    await userSettings.SetAzureSettings(new AzureSettings
                    {
                        TenantId = settings.TenantId,
                        ClientId = settings.ClientId,
                        ClientSecret = settings.ClientSecret,
                        SubscriptionId = settings.SubscriptionId,
                    });

                    WriteJson(new
                    {
                        resourceGroups,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandAzureSet);

                branch.AddAsyncDelegate<AzureDnsSettings>("dns", async (_, settings) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, settings.KeyPath, fallbackToSettings: true);
                    consoleLogger.Debug("Updating account on '{0}'.", serverUri);
                    var azureCredentials = await CreateAzureRestClient(BuildAzureSettings(settings));

                    var acme = contextFactory.Invoke(serverUri, key);
                    var orderCtx = acme.Order(settings.OrderId);
                    var authzCtx = await orderCtx.Authorization(settings.Domain)
                        ?? throw new CertesCliException(string.Format(Strings.ErrorIdentifierNotAvailable, settings.Domain));
                    var challengeCtx = await authzCtx.Dns()
                        ?? throw new CertesCliException(string.Format(Strings.ErrorChallengeNotAvailable, "dns"));

                    var authz = await authzCtx.Resource();
                    var dnsValue = acme.AccountKey.DnsTxt(challengeCtx.Token);
                    using var client = dnsClientFactory.Invoke(azureCredentials);
                    client.SubscriptionId = azureCredentials.Credentials.DefaultSubscriptionId;

                    var idValue = authz.Identifier.Value;
                    var zone = await FindDnsZone(client, idValue);
                    var name = zone.Name.Length == idValue.Length
                        ? "_acme-challenge"
                        : "_acme-challenge." + idValue.Substring(0, idValue.Length - zone.Name.Length - 1);

                    consoleLogger.Debug("Adding TXT record '{0}' for '{1}' in '{2}' zone.", dnsValue, name, zone.Name);
                    var recordSet = await client.RecordSets.CreateOrUpdateAsync(
                        settings.ResourceGroup,
                        zone.Name,
                        name,
                        RecordType.TXT,
                        new RecordSetInner(name: name, tTL: 300, txtRecords: new[] { new TxtRecord(new[] { dnsValue }) }));

                    WriteJson(new
                    {
                        data = recordSet,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandAzureDns);

                branch.AddAsyncDelegate<AzureAppSettings>("app", async (_, settings) =>
                {
                    var (serverUri, key) = await ReadAccountKey(settings.Server, settings.KeyPath, fallbackToSettings: true);
                    var azureCredentials = await CreateAzureRestClient(BuildAzureSettings(settings));
                    var privKey = await CommandBase.ReadKey(settings.PrivateKey, "CERTES_CERT_KEY", fileUtil, environmentVariables);
                    if (privKey == null)
                    {
                        throw new CertesCliException(Strings.ErrorNoPrivateKey);
                    }

                    var acme = contextFactory.Invoke(serverUri, key);
                    var orderCtx = acme.Order(settings.OrderId);
                    var order = await orderCtx.Resource();
                    if (order.Certificate == null)
                    {
                        throw new CertesCliException(string.Format(Strings.ErrorOrderIncompleted, orderCtx.Location));
                    }

                    var cert = await orderCtx.Download(settings.PreferredChain);
                    using var x509Cert = X509CertificateLoader.LoadCertificate(cert.Certificate.ToDer());
                    var thumbprint = x509Cert.Thumbprint;

                    using var client = webSiteClientFactory.Invoke(azureCredentials);
                    client.SubscriptionId = azureCredentials.Credentials.DefaultSubscriptionId;
                    var certUploaded = await FindCertificate(client, settings.ResourceGroup, thumbprint);
                    if (certUploaded == null)
                    {
                        certUploaded = await UploadCertificate(client, settings.ResourceGroup, settings.App, settings.Slot, cert.ToPfx(privKey), thumbprint);
                    }

                    var hostNameBinding = new HostNameBindingInner
                    {
                        SslState = SslState.SniEnabled,
                        Thumbprint = certUploaded.Thumbprint,
                    };

                    var hostName = string.IsNullOrWhiteSpace(settings.Slot)
                        ? await client.WebApps.CreateOrUpdateHostNameBindingAsync(settings.ResourceGroup, settings.App, settings.Domain, hostNameBinding)
                        : await client.WebApps.CreateOrUpdateHostNameBindingSlotAsync(settings.ResourceGroup, settings.App, settings.Domain, hostNameBinding, settings.Slot);

                    WriteJson(new
                    {
                        data = hostName,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandAzureApp);
            });
        }

        private async Task<(Uri Location, CertificateChain Cert)> DownloadCertificate(Uri orderUri, string preferredChain, Uri server, string keyPath)
        {
            var (serverUri, key) = await ReadAccountKey(server, keyPath, fallbackToSettings: true, required: true);

            consoleLogger.Debug("Downloading certificate from '{0}'.", serverUri);
            var acme = contextFactory.Invoke(serverUri, key);
            var orderCtx = acme.Order(orderUri);
            var order = await orderCtx.Resource();
            if (order.Status != OrderStatus.Valid)
            {
                throw new CertesCliException(string.Format(Strings.ErrorExportInvalidOrder, order.Status));
            }

            return (order.Certificate, await orderCtx.Download(preferredChain));
        }

        private async Task<RestClient> CreateAzureRestClient(AzureSettings options)
        {
            var azSettings = await userSettings.GetAzureSettings();
            var tenantId = options.TenantId ?? azSettings.TenantId;
            var clientId = options.ClientId ?? azSettings.ClientId;
            var secret = options.ClientSecret ?? azSettings.ClientSecret;
            var subscriptionId = options.SubscriptionId ?? azSettings.SubscriptionId;

            ValidateOption(tenantId, AzureCommandBase.AzureTenantIdOption);
            ValidateOption(clientId, AzureCommandBase.AzureClientIdOption);
            ValidateOption(secret, AzureCommandBase.AzureSecretOption);
            ValidateOption(subscriptionId, AzureCommandBase.AzureSubscriptionIdOption);

            var loginInfo = new ServicePrincipalLoginInformation
            {
                ClientId = clientId,
                ClientSecret = secret,
            };

            return RestClient.Configure()
                .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithCredentials(new AzureCredentials(loginInfo, tenantId, AzureEnvironment.AzureGlobalCloud)
                    .WithDefaultSubscription(subscriptionId))
                .Build();
        }

        private static void ValidateOption(string value, string optionName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new CertesCliException(string.Format(Strings.ErrorOptionMissing, optionName));
            }
        }

        private static AzureSettings BuildAzureSettings(AzureCredentialSettings settings)
        {
            return new AzureSettings
            {
                TenantId = settings.TenantId,
                ClientId = settings.ClientId,
                ClientSecret = settings.ClientSecret,
                SubscriptionId = settings.SubscriptionId,
            };
        }

        private async Task<IList<(string Location, string Name)>> LoadResourceGroups(RestClient restClient)
        {
            using var client = resourceClientFactory.Invoke(restClient);
            client.SubscriptionId = restClient.Credentials.DefaultSubscriptionId;
            var resourceGroups = await client.ResourceGroups.ListAsync();
            return resourceGroups.Select(g => (g.Location, g.Name)).ToArray();
        }

        private async Task<ZoneInner> FindDnsZone(IDnsManagementClient client, string identifier)
        {
            var zones = await client.Zones.ListAsync();
            while (zones != null)
            {
                foreach (var zone in zones)
                {
                    if (identifier.EndsWith($".{zone.Name}", StringComparison.OrdinalIgnoreCase) ||
                        identifier.Equals(zone.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        var options = new JsonSerializerOptions { WriteIndented = true };
                        consoleLogger.Debug("DNS zone:\n{0}", JsonSerializer.Serialize(zone, options));
                        return zone;
                    }
                }

                zones = string.IsNullOrWhiteSpace(zones.NextPageLink)
                    ? null
                    : await client.Zones.ListNextAsync(zones.NextPageLink);
            }

            throw new CertesCliException(string.Format(Strings.ErrorDnsZoneNotFound, identifier));
        }

        private static async Task<CertificateInner> UploadCertificate(
            IWebSiteManagementClient client,
            string resourceGroup,
            string appName,
            string appSlot,
            PfxBuilder pfx,
            string thumbprint)
        {
            var pfxName = string.Format(CultureInfo.InvariantCulture, "[certes] {0:yyyyMMddhhmmss}", DateTime.UtcNow);
            var pfxPassword = Guid.NewGuid().ToString("N");
            var pfxBytes = pfx.Build(pfxName, pfxPassword);

            var webApp = string.IsNullOrWhiteSpace(appSlot)
                ? await client.WebApps.GetAsync(resourceGroup, appName)
                : await client.WebApps.GetSlotAsync(resourceGroup, appName, appSlot);

            var certData = new CertificateInner
            {
                PfxBlob = pfxBytes,
                Password = pfxPassword,
                Location = webApp.Location,
            };

            return await client.Certificates.CreateOrUpdateAsync(resourceGroup, thumbprint, certData);
        }

        private static async Task<CertificateInner> FindCertificate(IWebSiteManagementClient client, string resourceGroup, string thumbprint)
        {
            var certificates = await client.Certificates.ListByResourceGroupAsync(resourceGroup);
            while (certificates != null)
            {
                foreach (var azCert in certificates)
                {
                    if (string.Equals(azCert.Thumbprint, thumbprint, StringComparison.OrdinalIgnoreCase))
                    {
                        return azCert;
                    }
                }

                certificates = certificates.NextPageLink == null
                    ? null
                    : await client.Certificates.ListByResourceGroupNextAsync(certificates.NextPageLink);
            }

            return null;
        }

        private static void WriteJson(object value)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, jsonSerializerSettings));
        }

        /// <summary>
        /// Gets the registered command metadata for documentation purposes.
        /// </summary>
        public IEnumerable<CommandMetadata> GetCommandMetadata()
        {
            return commands.Select(cmd => new CommandMetadata
            {
                Group = cmd.Group.Command,
                Name = cmd.GetCommandName(),
                Description = cmd.GetCommandDescription()
            });
        }
    }

    /// <summary>
    /// Metadata about a registered command, used for documentation and help.
    /// </summary>
    internal class CommandMetadata
    {
        public string Group { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    internal sealed class ServerSetSettings : CommandSettings
    {
        [CommandArgument(0, "<NEW_SERVER>")]
        public Uri NewServer { get; init; }
    }

    internal sealed class ServerShowSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }
    }

    internal sealed class AccountNewSettings : CommandSettings
    {
        [CommandArgument(0, "<email>")]
        public string Email { get; init; }

        [CommandOption("--out-path|--out <OUT_PATH>")]
        public string OutPath { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class AccountSetSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandArgument(0, "<key-path>")]
        public string KeyPath { get; init; }
    }

    internal sealed class AccountShowSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class AccountUpdateSettings : CommandSettings
    {
        [CommandArgument(0, "<email>")]
        public string Email { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class OrderNewSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }

        [CommandArgument(0, "<domains>")]
        public string[] Domains { get; init; }
    }

    internal sealed class OrderListSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class OrderShowSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }

        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }
    }

    internal sealed class OrderAuthzSettings : CommandSettings
    {
        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandArgument(1, "<domain>")]
        public string Domain { get; init; }

        [CommandArgument(2, "<challenge-type>")]
        public string ChallengeType { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class OrderValidateSettings : CommandSettings
    {
        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandArgument(1, "<domain>")]
        public string Domain { get; init; }

        [CommandArgument(2, "<challenge-type>")]
        public string ChallengeType { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class OrderFinalizeSettings : CommandSettings
    {
        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }

        [CommandOption("--dn <DN>")]
        public string Dn { get; init; }

        [CommandOption("--out-path|--out <OUT_PATH>")]
        public string OutPath { get; init; }

        [CommandOption("--private-key <PRIVATE_KEY>")]
        public string PrivateKey { get; init; }

        [CommandOption("--key-algorithm <KEY_ALGORITHM>")]
        public KeyAlgorithm KeyAlgorithm { get; init; } = KeyAlgorithm.ES256;
    }

    internal sealed class CertificatePemSettings : CommandSettings
    {
        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandOption("--preferred-chain <PREFERRED_CHAIN>")]
        public string PreferredChain { get; init; }

        [CommandOption("--out-path|--out <OUT_PATH>")]
        public string OutPath { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class CertificatePfxSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }

        [CommandOption("--out-path|--out <OUT_PATH>")]
        public string OutPath { get; init; }

        [CommandOption("--private-key <PRIVATE_KEY>")]
        public string PrivateKey { get; init; }

        [CommandOption("--friendly-name <FRIENDLY_NAME>")]
        public string FriendlyName { get; init; }

        [CommandOption("--issuer <ISSUER>")]
        public string Issuer { get; init; }

        [CommandOption("--preferred-chain <PREFERRED_CHAIN>")]
        public string PreferredChain { get; init; }

        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandArgument(1, "<password>")]
        public string Password { get; init; }
    }

    internal abstract class AzureCredentialSettings : CommandSettings
    {
        [CommandOption("--tenant-id <TENANT_ID>")]
        public string TenantId { get; init; }

        [CommandOption("--client-id <CLIENT_ID>")]
        public string ClientId { get; init; }

        [CommandOption("--client-secret <CLIENT_SECRET>")]
        public string ClientSecret { get; init; }

        [CommandOption("--subscription-id <SUBSCRIPTION_ID>")]
        public string SubscriptionId { get; init; }

        [CommandOption("--resource-group <RESOURCE_GROUP>")]
        public string ResourceGroup { get; init; }
    }

    internal sealed class AzureSetSettings : CommandSettings
    {
        [CommandOption("--tenant-id <TENANT_ID>")]
        public string TenantId { get; init; }

        [CommandOption("--client-id <CLIENT_ID>")]
        public string ClientId { get; init; }

        [CommandOption("--client-secret <CLIENT_SECRET>")]
        public string ClientSecret { get; init; }

        [CommandOption("--subscription-id <SUBSCRIPTION_ID>")]
        public string SubscriptionId { get; init; }
    }

    internal sealed class AzureDnsSettings : AzureCredentialSettings
    {
        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandArgument(1, "<domain>")]
        public string Domain { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

    internal sealed class AzureAppSettings : AzureCredentialSettings
    {
        [CommandOption("--slot <SLOT>")]
        public string Slot { get; init; }

        [CommandOption("--private-key <PRIVATE_KEY>")]
        public string PrivateKey { get; init; }

        [CommandOption("--preferred-chain <PREFERRED_CHAIN>")]
        public string PreferredChain { get; init; }

        [CommandArgument(0, "<order-id>")]
        public Uri OrderId { get; init; }

        [CommandArgument(1, "<domain>")]
        public string Domain { get; init; }

        [CommandArgument(2, "<app>")]
        public string App { get; init; }

        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }

        [CommandOption("--key-path|--key|-k <KEY_PATH>")]
        public string KeyPath { get; init; }
    }

}

using System;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Cli.Options;
using Certes.Cli.Settings;
using Certes.Pkcs;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Newtonsoft.Json;
using NLog;

namespace Certes.Cli.Processors
{
    internal class AzureCommand
    {
        public ILogger Logger { get; } = LogManager.GetCurrentClassLogger();
        private AzureOptions Args { get; }

        public AzureCommand(AzureOptions args)
        {
            Args = args;
        }

        public static AzureOptions TryParse(ArgumentSyntax syntax)
        {
            var options = new AzureOptions();

            var command = Command.Undefined;
            syntax.DefineCommand("azure", ref command, Command.Azure, "Deploy to Azure.");
            if (command == Command.Undefined)
            {
                return null;
            }

            syntax.DefineOption("user", ref options.UserName, $"Azure user name or client ID.");
            syntax.DefineOption("pwd", ref options.Password, $"Azure password or client secret.");
            syntax.DefineOption<Guid>("talent", ref options.Talent, $"Azure talent ID.");
            syntax.DefineOption<Guid>("subscription", ref options.Subscription, $"Azure subscription ID.");
            syntax.DefineOption<Uri>("order", ref options.OrderUri, $"ACME order URI.");
            syntax.DefineEnumOption("cloud", ref options.CloudEnvironment, $"ACME order URI.");
            syntax.DefineOption("resourceGroup", ref options.ResourceGroup, $"The resource group.");
            syntax.DefineOption("cert-key", ref options.PrivateKey, $"The certificate private key.");
            syntax.DefineOption("app", ref options.AppServiceName, $"The app service name.");
            syntax.DefineOption("slot", ref options.Slot, $"The deployment slot.");
            syntax.DefineOptionList("issuer", ref options.Issuers, $"The issuer certificates.");

            syntax.DefineOption<Uri>("server", ref options.Server, $"ACME Directory Resource URI.");
            syntax.DefineOption("key", ref options.Path, $"File path to the account key to use.");

            syntax.DefineEnumParameter("action", ref options.Action, "Order action");
            syntax.DefineParameter("name", ref options.Value, "Domain name");

            return options;
        }

        public async Task<object> Process()
        {
            switch (Args.Action)
            {
                case AzureAction.Dns:
                    return await SetDns();
                case AzureAction.Ssl:
                    return await SetSslBinding();
            }

            throw new NotSupportedException();
        }

        private async Task<object> SetSslBinding()
        {
            var key = await UserSettings.GetAccountKey(Args, true);
            Logger.Debug("Using ACME server {0}.", Args.Server);
            var ctx = ContextFactory.Create(Args.Server, key);

            var orderCtx = ctx.Order(Args.OrderUri);

            var order = await orderCtx.Resource();

            byte[] pfx = null;
            var pfxPwd = Guid.NewGuid().ToString("N");
            var pfxName = order.Identifiers[0].Value;

            var hasCertKey = File.Exists(Args.PrivateKey);
            var certKey = hasCertKey ? 
                KeyFactory.FromPem(await FileUtil.ReadAllText(Args.PrivateKey)) :
                KeyFactory.NewKey(KeyAlgorithm.ES256);
            if (!hasCertKey)
            {
                await FileUtil.WriteAllTexts(Args.PrivateKey, certKey.ToPem());
            }

            byte[] issuers = null;
            if (Args.Issuers?.Count > 0)
            {
                using (var buffer = new MemoryStream())
                {
                    foreach (var issuer in Args.Issuers)
                    {
                        using (var stream = File.OpenRead(issuer))
                        {
                            stream.CopyTo(buffer);
                        }
                    }

                    issuers = buffer.ToArray();
                }
            }

            // try to finalize the order
            if (order.Status == OrderStatus.Pending)
            {
                var cert = await orderCtx.Generate(new CsrInfo
                {
                    CommonName = pfxName
                }, certKey);

                pfx = cert.ToPfx(pfxName, pfxPwd, issuers: issuers);
            }
            else
            {
                var certChain = await orderCtx.Download();
                var pfxBuilder = certChain.ToPfx(certKey);
                if (issuers != null)
                {
                    pfxBuilder.AddIssuers(issuers);
                }

                pfx = pfxBuilder.Build(pfxName, pfxPwd);
            }

            var certData = new CertificateInner
            {
                PfxBlob = pfx,
                Password = pfxPwd,
            };

            var azureSettings = await UserSettings.GetAzureSettings(Args);
            var credentials = GetAuzreCredentials(azureSettings);
            using (var client = ContextFactory.CreateAppServiceManagementClient(credentials))
            {
                client.SubscriptionId = azureSettings.SubscriptionId.ToString();

                certData = await client.Certificates.CreateOrUpdateAsync(Args.ResourceGroup, pfxName, certData);

                var hostNameBinding = new HostNameBindingInner
                {
                    SslState = SslState.SniEnabled,
                    Thumbprint = certData.Thumbprint,
                };
                
                var hostName = string.IsNullOrWhiteSpace(Args.Slot) ?
                    await client.WebApps.CreateOrUpdateHostNameBindingAsync(
                        Args.ResourceGroup, Args.AppServiceName, Args.Value, hostNameBinding) :
                    await client.WebApps.CreateOrUpdateHostNameBindingSlotAsync(
                            Args.ResourceGroup, Args.AppServiceName, Args.Value, hostNameBinding, Args.Slot);

                return hostName;
            }
        }

        private async Task<object> SetDns()
        {
            var key = await UserSettings.GetAccountKey(Args, true);

            Logger.Debug("Using ACME server {0}.", Args.Server);
            var ctx = ContextFactory.Create(Args.Server, key);

            var orderCtx = ctx.Order(Args.OrderUri);
            var authzCtx = await orderCtx.Authorization(Args.Value);
            if (authzCtx == null)
            {
                throw new Exception($"Authz not found for {Args.Value}.");
            }

            var authz = await authzCtx.Resource();
            var idValue = authz.Identifier.Value;

            var challengeCtx = await authzCtx.Dns();
            if (challengeCtx == null)
            {
                throw new Exception($"DNS challenge for {Args.Value} not found.");
            }

            var dnsValue = ctx.AccountKey.DnsTxt(challengeCtx.Token);

            var azureSettings = await UserSettings.GetAzureSettings(Args);
            var credentials = GetAuzreCredentials(azureSettings);
            using (var client = ContextFactory.CreateDnsManagementClient(credentials))
            {
                client.SubscriptionId = azureSettings.SubscriptionId.ToString();

                ZoneInner idZone = null;
                var zones = await client.Zones.ListAsync();
                while (idZone == null && zones != null)
                {
                    foreach (var zone in zones)
                    {
                        if (idValue.EndsWith($".{zone.Name}"))
                        {
                            idZone = zone;
                            break;
                        }
                    }

                    zones = string.IsNullOrWhiteSpace(zones.NextPageLink) ? null : await client.Zones.ListNextAsync(zones.NextPageLink);
                }

                if (idZone == null)
                {
                    throw new Exception($"DNS zone for name {Args.Value} not found.");
                }
                else
                {
                    Logger.Debug("DNS zone:\n{0}", JsonConvert.SerializeObject(idZone, Formatting.Indented));
                }

                var name = "_acme-challenge." + idValue.Substring(0, idValue.Length - idZone.Name.Length - 1);
                Logger.Debug("Adding TXT record '{0}' for '{1}' in '{2}' zone.", dnsValue, name, idZone.Name);

                var recordSet = await client.RecordSets.CreateOrUpdateAsync(
                    Args.ResourceGroup,
                    idZone.Name,
                    name,
                    RecordType.TXT,
                    new RecordSetInner(
                        name: name,
                        tTL: 300,
                        txtRecords: new[] { new TxtRecord(new[] { dnsValue }) }));

                return new
                {
                    data = recordSet
                };
            }
        }

        private AzureCredentials GetAuzreCredentials(AzureSettings settings)
        {
            var loginInfo = new ServicePrincipalLoginInformation
            {
                ClientId = settings.ClientId,
                ClientSecret = settings.Secret,
            };

            var env =
                settings.Environment == AzureCloudEnvironment.China ? AzureEnvironment.AzureChinaCloud :
                settings.Environment == AzureCloudEnvironment.German ? AzureEnvironment.AzureGermanCloud :
                settings.Environment == AzureCloudEnvironment.USGovernment ? AzureEnvironment.AzureUSGovernment :
                AzureEnvironment.AzureGlobalCloud;

            var credentials = new AzureCredentials(loginInfo, settings.Talent.ToString(), env);
            return credentials;
        }
    }
}

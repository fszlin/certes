using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Certes.Pkcs;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;

namespace Certes.Cli.Commands
{
    class AzureAppCommand : AzureCommandBase, ICliCommand
    {
        private const string CommandText = "app";
        private const string OrderIdParam = "order-id";
        private const string PreferredChainOption = "--preferred-chain";
        private const string AppNameParam = "app";
        private const string SlotOption = "--slot";
        private const string PrivateKeyOption = "--private-key";
        private const string DomainParam = "domain";

        public CommandGroup Group => CommandGroup.Azure;

        private readonly IEnvironmentVariables environment;
        private readonly AzureClientFactory<IWebSiteManagementClient> clientFactory;

        public AzureAppCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil,
            IEnvironmentVariables environment,
            AzureClientFactory<IWebSiteManagementClient> clientFactory)
            : base(userSettings, contextFactory, fileUtil)
        {
            this.clientFactory = clientFactory;
            this.environment = environment;
        }

        public Command Define()
        {
            var cmd = new Command(CommandText, Strings.HelpCommandAzureApp)
            {
                new Option<string>(SlotOption, Strings.HelpSlot),
                new Option<string>(PrivateKeyOption, Strings.HelpPrivateKey),
                new Option<string>(PreferredChainOption, Strings.HelpPreferredChain),
                new Argument<Uri>(OrderIdParam, Strings.HelpOrderId),
                new Argument<string>(DomainParam, Strings.HelpDomain),
                new Argument<string>(AppNameParam, Strings.HelpAppName),
            };

            AddCommonOptions(cmd);

            cmd.Handler = CommandHandler.Create(async (
                Uri orderId,
                string domain,
                string app,
                string slot,
                string preferredChain,
                string privateKey,

                Uri server,
                string keyPath,
                AzureOptions azureOptions,
                IConsole console) =>
            {
                var (serverUri, key) = await ReadAccountKey(server, keyPath, true, false);
                var azureCredentials = await CreateAzureRestClient(azureOptions);

                var privKey = await ReadKey(privateKey, "CERTES_CERT_KEY", File, environment);
                if (privKey == null)
                {
                    throw new CertesCliException(Strings.ErrorNoPrivateKey);
                }

                var acme = ContextFactory.Invoke(serverUri, key);
                var orderCtx = acme.Order(orderId);

                var order = await orderCtx.Resource();
                if (order.Certificate == null)
                {
                    throw new CertesCliException(string.Format(Strings.ErrorOrderIncompleted, orderCtx.Location));
                }

                var cert = await orderCtx.Download(preferredChain);
                var x509Cert = new X509Certificate2(cert.Certificate.ToDer());
                var thumbprint = x509Cert.Thumbprint;

                using var client = clientFactory.Invoke(azureCredentials);
                client.SubscriptionId = azureCredentials.Credentials.DefaultSubscriptionId;
                var certUploaded = await FindCertificate(client, azureOptions.ResourceGroup, thumbprint);
                if (certUploaded == null)
                {
                    certUploaded = await UploadCertificate(
                        client, azureOptions.ResourceGroup, app, slot, cert.ToPfx(privKey), thumbprint);
                }

                var hostNameBinding = new HostNameBindingInner
                {
                    SslState = SslState.SniEnabled,
                    Thumbprint = certUploaded.Thumbprint,
                };

                var hostName = string.IsNullOrWhiteSpace(slot) ?
                    await client.WebApps.CreateOrUpdateHostNameBindingAsync(
                        azureOptions.ResourceGroup, app, domain, hostNameBinding) :
                    await client.WebApps.CreateOrUpdateHostNameBindingSlotAsync(
                        azureOptions.ResourceGroup, app, domain, hostNameBinding, slot);

                var output = new
                {
                    data = hostName
                };

                console.WriteAsJson(output);
            });

            return cmd;
        }

        private static async Task<CertificateInner> UploadCertificate(
            IWebSiteManagementClient client, string resourceGroup, string appName, string appSlot, PfxBuilder pfx, string thumbprint)
        {
            var pfxName = string.Format(CultureInfo.InvariantCulture, "[certes] {0:yyyyMMddhhmmss}", DateTime.UtcNow);
            var pfxPassword = Guid.NewGuid().ToString("N");
            var pfxBytes = pfx.Build(pfxName, pfxPassword);

            var webApp = string.IsNullOrWhiteSpace(appSlot) ?
                await client.WebApps.GetAsync(resourceGroup, appName) :
                await client.WebApps.GetSlotAsync(resourceGroup, appName, appSlot);

            var certData = new CertificateInner
            {
                PfxBlob = pfxBytes,
                Password = pfxPassword,
                Location = webApp.Location,
            };

            return await client.Certificates.CreateOrUpdateAsync(
                resourceGroup, thumbprint, certData);
        }

        private static async Task<CertificateInner> FindCertificate(
            IWebSiteManagementClient client, string resourceGroup, string thumbprint)
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

                certificates = certificates.NextPageLink == null ? null :
                    await client.Certificates.ListByResourceGroupNextAsync(certificates.NextPageLink);
            }

            return null;
        }
    }
}

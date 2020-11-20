using System;
using System.CommandLine;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Certes.Pkcs;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;

namespace Certes.Cli.Commands
{
    class AzureAppCommand : AzureCommand, ICliCommand
    {
        private const string CommandText = "app";
        private const string OrderIdParam = "order-id";
        private const string PreferredChainOption = "preferred-chain";
        private const string AppNameParam = "app";
        private const string SlotOption = "slot";
        private const string PrivateKeyOption = "private-key";
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

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandAzureApp);

            DefineAzureOptions(syntax)
                .DefineOption(SlotOption, help: Strings.HelpSlot)
                .DefineOption(PrivateKeyOption, help: Strings.HelpPrivateKey)
                .DefineOption(PreferredChainOption, help: Strings.HelpPreferredChain)
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId)
                .DefineParameter(DomainParam, help: Strings.HelpDomain)
                .DefineParameter(AppNameParam, help: Strings.HelpAppName);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, false);
            var orderUri = syntax.GetParameter<Uri>(OrderIdParam, true);
            var domain = syntax.GetParameter<string>(DomainParam, true);

            var azureCredentials = await CreateAzureRestClient(syntax);
            var resourceGroup = syntax.GetOption<string>(AzureResourceGroupOption, true);
            var appName = syntax.GetParameter<string>(AppNameParam, true);
            var appSlot = syntax.GetOption<string>(SlotOption, false);
            var preferredChain = syntax.GetOption<string>(PreferredChainOption);

            var privKey = await syntax.ReadKey(PrivateKeyOption, "CERTES_CERT_KEY", File, environment, true);

            var acme = ContextFactory.Invoke(serverUri, key);
            var orderCtx = acme.Order(orderUri);

            var order = await orderCtx.Resource();
            if (order.Certificate == null)
            {
                throw new CertesCliException(string.Format(Strings.ErrorOrderIncompleted, orderCtx.Location));
            }

            var cert = await orderCtx.Download(preferredChain);
            var x509Cert = new X509Certificate2(cert.Certificate.ToDer());
            var thumbprint = x509Cert.Thumbprint;

            using (var client = clientFactory.Invoke(azureCredentials))
            {
                client.SubscriptionId = azureCredentials.Credentials.DefaultSubscriptionId;
                var certUploaded = await FindCertificate(client, resourceGroup, thumbprint);
                if (certUploaded == null)
                {
                    certUploaded = await UploadCertificate(
                        client, resourceGroup, appName, appSlot, cert.ToPfx(privKey), thumbprint);
                }

                var hostNameBinding = new HostNameBindingInner
                {
                    SslState = SslState.SniEnabled,
                    Thumbprint = certUploaded.Thumbprint,
                };

                var hostName = string.IsNullOrWhiteSpace(appSlot) ?
                    await client.WebApps.CreateOrUpdateHostNameBindingAsync(
                        resourceGroup, appName, domain, hostNameBinding) :
                    await client.WebApps.CreateOrUpdateHostNameBindingSlotAsync(
                        resourceGroup, appName, domain, hostNameBinding, appSlot);

                return new
                {
                    data = hostName
                };
            }
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

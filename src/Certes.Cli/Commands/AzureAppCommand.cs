using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;

namespace Certes.Cli.Commands
{
    class AzureAppCommand : AzureCommand, ICliCommand
    {
        private const string CommandText = "app";
        private const string OrderIdParam = "order-id";
        private const string AppNameParam = "app";
        private const string SlotOption = "app-slot";
        private const string PrivateKeyParam = "private-key";
        private const string DomainParam = "domain";

        public CommandGroup Group => throw new NotImplementedException();

        private readonly IAppServiceClientFactory clientFactory;

        public AzureAppCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil,
            IAppServiceClientFactory clientFactory)
            : base(userSettings, contextFactory, fileUtil)
        {
            this.clientFactory = clientFactory;
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandAzureApp);

            DefineAzureOptions(syntax)
                .DefineOption(SlotOption, help: Strings.HelpSlot)
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId)
                .DefineParameter(DomainParam, help: Strings.HelpDomain)
                .DefineParameter(AppNameParam, help: Strings.HelpAppName)
                .DefineParameter(PrivateKeyParam, help: Strings.HelpPrivateKey);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, false);
            var orderUri = syntax.GetParameter<Uri>(OrderIdParam, true);
            var domain = syntax.GetParameter<string>(DomainParam, true);

            var azureCredentials = await ReadAzureCredentials(syntax);
            var resourceGroup = syntax.GetOption<string>(AzureResourceGroupOption, true);
            var appName = syntax.GetParameter<string>(AppNameParam, true);
            var appSlot = syntax.GetOption<string>(SlotOption, false);

            var keyPath = syntax.GetParameter<string>(PrivateKeyParam, true);
            
            var acme = ContextFactory.Create(serverUri, key);
            var orderCtx = acme.Order(orderUri);

            var order = await orderCtx.Resource();
            if (order.Certificate == null)
            {
                throw new Exception(string.Format(Strings.ErrorOrderIncompleted, orderCtx.Location));
            }

            var cert = await orderCtx.Download();

            var pfxName = $"{order.Certificate} by certes";
            var privKey = KeyFactory.FromPem(await File.ReadAllText(keyPath));
            var pfxPassword = Guid.NewGuid().ToString("N");
            var pfx = cert.ToPfx(privKey);
            var pfxBytes = pfx.Build(pfxName, pfxPassword);

            var certData = new CertificateInner
            {
                PfxBlob = pfxBytes,
                Password = pfxPassword,
            };

            using (var client = clientFactory.Create(azureCredentials))
            {
                client.SubscriptionId = azureCredentials.DefaultSubscriptionId;

                var certUpdated = await client.Certificates.CreateOrUpdateAsync(
                    resourceGroup, pfxName, certData);

                var hostNameBinding = new HostNameBindingInner
                {
                    SslState = SslState.SniEnabled,
                    Thumbprint = certUpdated.Thumbprint,
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
    }
}

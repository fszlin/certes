using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Newtonsoft.Json;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AzureDnsCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "dns";
        private const string OrderIdParam = "order-id";
        private const string DomainParam = "domain";

        private const string AzureTalentIdOption = "talent-id";
        private const string AzureClientIdOption = "client-id";
        private const string AzureSecretOption = "client-secret";
        private const string AzureSubscriptionIdOption = "subscription-id";
        private const string AzureResourceGroupOption = "resource-group";

        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountUpdateCommand));

        private readonly IDnsClientFactory clientFactory;

        public CommandGroup Group { get; } = CommandGroup.Azure;

        public AzureDnsCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil,
            IDnsClientFactory clientFactory)
            : base(userSettings, contextFactory, fileUtil)
        {
            this.clientFactory = clientFactory;
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandAzureDns);

            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineOption(AzureTalentIdOption, help: Strings.HelpAzureTalentId)
                .DefineOption(AzureClientIdOption, help: Strings.HelpAzureClientId)
                .DefineOption(AzureSecretOption, help: Strings.HelpAzureSecret)
                .DefineOption(AzureSubscriptionIdOption, help: Strings.HelpAzureSubscriptionId)
                .DefineOption(AzureResourceGroupOption, help: Strings.HelpAzureResourceGroup)
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId)
                .DefineParameter(DomainParam, help: Strings.HelpDomain);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, false);
            var orderUri = syntax.GetParameter<Uri>(OrderIdParam, true);
            var domain = syntax.GetParameter<string>(DomainParam, true);
            var azureCredentials = await ReadAzureCredentials(syntax);
            var resourceGroup = syntax.GetOption<string>(AzureResourceGroupOption, true);

            var acme = ContextFactory.Create(serverUri, key);
            var orderCtx = acme.Order(orderUri);
            var authzCtx = await orderCtx.Authorization(domain)
                ?? throw new Exception(string.Format(Strings.ErrorIdentifierNotAvailable, domain));
            var challengeCtx = await authzCtx.Dns()
                ?? throw new Exception(string.Format(Strings.ErrorChallengeNotAvailable, "dns"));

            var authz = await authzCtx.Resource();
            var dnsValue = acme.AccountKey.DnsTxt(challengeCtx.Token);
            using (var client = clientFactory.Create(azureCredentials))
            {
                client.SubscriptionId = azureCredentials.DefaultSubscriptionId;
                var idValue = authz.Identifier.Value;
                var zone = await FindDnsZone(client, idValue);

                var name = "_acme-challenge." + idValue.Substring(0, idValue.Length - zone.Name.Length - 1);
                logger.Debug("Adding TXT record '{0}' for '{1}' in '{2}' zone.", dnsValue, name, zone.Name);

                var recordSet = await client.RecordSets.CreateOrUpdateAsync(
                    resourceGroup,
                    zone.Name,
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

        private static async Task<ZoneInner> FindDnsZone(IDnsManagementClient client, string identifier)
        {
            var zones = await client.Zones.ListAsync();
            while (zones != null)
            {
                foreach (var zone in zones)
                {
                    if (identifier.EndsWith($".{zone.Name}"))
                    {
                        logger.Debug(() => 
                            string.Format("DNS zone:\n{0}", JsonConvert.SerializeObject(zone, Formatting.Indented)));
                        return zone;
                    }
                }

                zones = string.IsNullOrWhiteSpace(zones.NextPageLink) ? 
                    null : 
                    await client.Zones.ListNextAsync(zones.NextPageLink);
            }

            throw new Exception(string.Format(Strings.ErrorDnsZoneNotFound, identifier));
        }

        protected Task<AzureCredentials> ReadAzureCredentials(ArgumentSyntax syntax)
        {
            var talentId = syntax.GetOption<string>(AzureTalentIdOption, true);
            var clientId = syntax.GetOption<string>(AzureClientIdOption, true);
            var secret = syntax.GetOption<string>(AzureSecretOption, true);
            var subscriptionId = syntax.GetOption<string>(AzureSubscriptionIdOption, true);

            var loginInfo = new ServicePrincipalLoginInformation
            {
                ClientId = clientId,
                ClientSecret = secret,
            };

            var credentials = new AzureCredentials(
                loginInfo, talentId, AzureEnvironment.AzureGlobalCloud)
                .WithDefaultSubscription(subscriptionId);

            return Task.FromResult(credentials);
        }
    }
}

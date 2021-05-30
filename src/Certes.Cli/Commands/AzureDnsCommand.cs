using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AzureDnsCommand : AzureCommandBase, ICliCommand
    {
        private const string CommandText = "dns";
        private const string OrderIdParam = "order-id";
        private const string DomainParam = "domain";

        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountUpdateCommand));

        private readonly AzureClientFactory<IDnsManagementClient> clientFactory;

        public CommandGroup Group { get; } = CommandGroup.Azure;

        public AzureDnsCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil,
            AzureClientFactory<IDnsManagementClient> clientFactory)
            : base(userSettings, contextFactory, fileUtil)
        {
            this.clientFactory = clientFactory;
        }

        public Command Define()
        {
            var cmd = new Command(CommandText, Strings.HelpCommandAzureDns)
            {
                new Argument<Uri>(OrderIdParam, Strings.HelpOrderId),
                new Argument<string>(DomainParam, Strings.HelpDomain),
            };

            cmd = AddCommonOptions(cmd);

            cmd.Handler = CommandHandler.Create(async (
                Uri orderId,
                string domain,
                Uri server,
                string keyPath,
                AzureOptions azureOptions,
                IConsole console) =>
            {
                var (serverUri, key) = await ReadAccountKey(server, keyPath, true, false);
                logger.Debug("Updating account on '{0}'.", serverUri);
                var azureCredentials = await CreateAzureRestClient(azureOptions);
                var resourceGroup = azureOptions.ResourceGroup;

                var acme = ContextFactory.Invoke(serverUri, key);
                var orderCtx = acme.Order(orderId);
                var authzCtx = await orderCtx.Authorization(domain)
                    ?? throw new CertesCliException(string.Format(Strings.ErrorIdentifierNotAvailable, domain));
                var challengeCtx = await authzCtx.Dns()
                    ?? throw new CertesCliException(string.Format(Strings.ErrorChallengeNotAvailable, "dns"));

                var authz = await authzCtx.Resource();
                var dnsValue = acme.AccountKey.DnsTxt(challengeCtx.Token);
                using var client = clientFactory.Invoke(azureCredentials);

                client.SubscriptionId = azureCredentials.Credentials.DefaultSubscriptionId;
                var idValue = authz.Identifier.Value;
                var zone = await FindDnsZone(client, idValue);

                var name = zone.Name.Length == idValue.Length ?
                    "_acme-challenge" :
                    "_acme-challenge." + idValue.Substring(0, idValue.Length - zone.Name.Length - 1);
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

                var output = new
                {
                    data = recordSet
                };

                console.WriteAsJson(output);
            });

            return cmd;
        }

        private static async Task<ZoneInner> FindDnsZone(IDnsManagementClient client, string identifier)
        {
            var zones = await client.Zones.ListAsync();
            while (zones != null)
            {
                foreach (var zone in zones)
                {
                    if (identifier.EndsWith($".{zone.Name}", StringComparison.OrdinalIgnoreCase) ||
                        identifier.Equals(zone.Name, StringComparison.OrdinalIgnoreCase))
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

            throw new CertesCliException(string.Format(Strings.ErrorDnsZoneNotFound, identifier));
        }
    }
}

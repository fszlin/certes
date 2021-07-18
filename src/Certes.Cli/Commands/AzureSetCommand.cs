using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Certes.Cli.Commands
{
    internal class AzureSetCommand : AzureCommandBase, ICliCommand
    {
        private const string CommandText = "set";

        private readonly AzureClientFactory<IResourceManagementClient> clientFactory;
        public CommandGroup Group => CommandGroup.Azure;

        public AzureSetCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil,
            AzureClientFactory<IResourceManagementClient> clientFactory)
            : base(userSettings, contextFactory, fileUtil)
        {
            this.clientFactory = clientFactory;
        }

        public Command Define()
        {
            var cmd = new Command(CommandText, Strings.HelpCommandAzureSet)
            {
                new Option<string>(AzureTenantIdOption, Strings.HelpAzureTenantId),
                new Option<string>(AzureClientIdOption, Strings.HelpAzureClientId),
                new Option<string>(AzureSecretOption, Strings.HelpAzureSecret),
                new Option<string>(AzureSubscriptionIdOption, Strings.HelpAzureSubscriptionId),
            };

            cmd.Handler = CommandHandler.Create(async (
                AzureSettings azureOptions,
                IConsole console) =>
            {
                var loginInfo = new ServicePrincipalLoginInformation
                {
                    ClientId = azureOptions.ClientId,
                    ClientSecret = azureOptions.ClientSecret,
                };

                var credentials = new AzureCredentials(
                    loginInfo, azureOptions.TenantId, AzureEnvironment.AzureGlobalCloud)
                    .WithDefaultSubscription(azureOptions.SubscriptionId);

                var builder = RestClient.Configure();
                var resClient = builder.WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                    .WithCredentials(credentials)
                    .Build();

                var resourceGroups = await LoadResourceGroups(resClient);

                await UserSettings.SetAzureSettings(azureOptions);

                var output = new
                {
                    resourceGroups
                };

                console.WriteAsJson(output);
            });

            return cmd;
        }

        private async Task<IList<(string Location, string Name)>> LoadResourceGroups(RestClient restClient)
        {
            using var client = clientFactory.Invoke(restClient);
            client.SubscriptionId = restClient.Credentials.DefaultSubscriptionId;
            var resourceGroups = await client.ResourceGroups.ListAsync();
            return resourceGroups.Select(g => (g.Location, g.Name)).ToArray();
        }
    }
}

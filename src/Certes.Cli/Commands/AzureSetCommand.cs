using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using NLog;
using static Certes.Cli.Commands.AzureCommand;

namespace Certes.Cli.Commands
{
    internal class AzureSetCommand : ICliCommand
    {
        private const string CommandText = "set";

        private readonly AzureClientFactory<IResourceManagementClient> clientFactory;
        private readonly IUserSettings userSettings;

        public CommandGroup Group => CommandGroup.Azure;

        public AzureSetCommand(IUserSettings userSettings, AzureClientFactory<IResourceManagementClient> clientFactory)
        {
            this.userSettings = userSettings;
            this.clientFactory = clientFactory;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var tenantId = syntax.GetOption<string>(AzureTenantIdOption);
            var clientId = syntax.GetOption<string>(AzureClientIdOption);
            var secret = syntax.GetOption<string>(AzureSecretOption);
            var subscriptionId = syntax.GetOption<string>(AzureSubscriptionIdOption);

            var loginInfo = new ServicePrincipalLoginInformation
            {
                ClientId = clientId,
                ClientSecret = secret,
            };

            var credentials = new AzureCredentials(
                loginInfo, tenantId, AzureEnvironment.AzureGlobalCloud)
                .WithDefaultSubscription(subscriptionId);

            var resourceGroups = await LoadResourceGroups(credentials);

            var azSettings = new AzureSettings
            {
                ClientId = clientId,
                TenantId = tenantId,
                ClientSecret = secret,
                SubscriptionId = subscriptionId,
            };

            await userSettings.SetAzureSettings(azSettings);

            return new
            {
                resourceGroups
            };
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandAzureSet);
            syntax
                .DefineOption(AzureTenantIdOption, help: Strings.HelpAzureTenantId)
                .DefineOption(AzureClientIdOption, help: Strings.HelpAzureClientId)
                .DefineOption(AzureSecretOption, help: Strings.HelpAzureSecret)
                .DefineOption(AzureSubscriptionIdOption, help: Strings.HelpAzureSubscriptionId);

            return cmd;
        }

        private async Task<IList<(string Location, string Name)>> LoadResourceGroups(AzureCredentials credentials)
        {
            using (var client = clientFactory.Invoke(credentials))
            {
                var resourceGroups = await client.ResourceGroups.ListAsync();
                return resourceGroups.Select(g => (g.Location, g.Name)).ToArray();
            }
        }
    }
}

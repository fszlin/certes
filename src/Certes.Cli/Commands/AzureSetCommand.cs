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

        private readonly IResourceClientFactory clientFactory;
        private readonly IUserSettings userSettings;

        public CommandGroup Group => CommandGroup.Azure;

        public AzureSetCommand(IUserSettings userSettings, IResourceClientFactory clientFactory)
        {
            this.userSettings = userSettings;
            this.clientFactory = clientFactory;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var talentId = syntax.GetOption<string>(AzureTalentIdOption);
            var clientId = syntax.GetOption<string>(AzureClientIdOption);
            var secret = syntax.GetOption<string>(AzureSecretOption);
            var subscriptionId = syntax.GetOption<string>(AzureSubscriptionIdOption);

            var loginInfo = new ServicePrincipalLoginInformation
            {
                ClientId = clientId,
                ClientSecret = secret,
            };

            var credentials = new AzureCredentials(
                loginInfo, talentId, AzureEnvironment.AzureGlobalCloud)
                .WithDefaultSubscription(subscriptionId);

            var resourceGroups = await LoadResourceGroups(credentials);

            var azSettings = new AzureSettings
            {
                ClientId = clientId,
                TalentId = talentId,
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
                .DefineOption(AzureTalentIdOption, help: Strings.HelpAzureTalentId)
                .DefineOption(AzureClientIdOption, help: Strings.HelpAzureClientId)
                .DefineOption(AzureSecretOption, help: Strings.HelpAzureSecret)
                .DefineOption(AzureSubscriptionIdOption, help: Strings.HelpAzureSubscriptionId);

            return cmd;
        }

        private async Task<IList<(string Location, string Name)>> LoadResourceGroups(AzureCredentials credentials)
        {
            using (var client = clientFactory.Create(credentials))
            {
                var resourceGroups = await client.ResourceGroups.ListAsync();
                return resourceGroups.Select(g => (g.Location, g.Name)).ToArray();
            }
        }
    }
}

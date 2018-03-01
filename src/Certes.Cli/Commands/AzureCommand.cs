using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace Certes.Cli.Commands
{
    internal abstract class AzureCommand : CommandBase
    {
        protected const string AzureResourceGroupOption = "resource-group";

        public static string AzureTalentIdOption => "talent-id";
        public static string AzureClientIdOption => "client-id";
        public static string AzureSecretOption => "client-secret";
        public static string AzureSubscriptionIdOption => "subscription-id";

        protected AzureCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        protected async Task<AzureCredentials> ReadAzureCredentials(ArgumentSyntax syntax)
        {
            var azSettings = await UserSettings.GetAzureSettings();
            var talentId = syntax.GetOption<string>(AzureTalentIdOption)
                ?? azSettings.TalentId;
            var clientId = syntax.GetOption<string>(AzureClientIdOption)
                ?? azSettings.ClientId;
            var secret = syntax.GetOption<string>(AzureSecretOption)
                ?? azSettings.ClientSecret;
            var subscriptionId = syntax.GetOption<string>(AzureSubscriptionIdOption)
                ?? azSettings.SubscriptionId;

            var loginInfo = new ServicePrincipalLoginInformation
            {
                ClientId = clientId,
                ClientSecret = secret,
            };

            var credentials = new AzureCredentials(
                loginInfo, talentId, AzureEnvironment.AzureGlobalCloud)
                .WithDefaultSubscription(subscriptionId);

            return credentials;
        }

        protected static ArgumentSyntax DefineAzureOptions(ArgumentSyntax syntax)
        {
            return syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineOption(AzureTalentIdOption, help: Strings.HelpAzureTalentId)
                .DefineOption(AzureClientIdOption, help: Strings.HelpAzureClientId)
                .DefineOption(AzureSecretOption, help: Strings.HelpAzureSecret)
                .DefineOption(AzureSubscriptionIdOption, help: Strings.HelpAzureSubscriptionId)
                .DefineOption(AzureResourceGroupOption, help: Strings.HelpAzureResourceGroup);
        }
    }
}

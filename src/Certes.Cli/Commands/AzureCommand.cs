using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Certes.Cli.Commands
{
    internal abstract class AzureCommand : CommandBase
    {
        protected const string AzureResourceGroupOption = "resource-group";

        public static string AzureTenantIdOption => "tenant-id";
        public static string AzureClientIdOption => "client-id";
        public static string AzureSecretOption => "client-secret";
        public static string AzureSubscriptionIdOption => "subscription-id";

        protected AzureCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        protected async Task<RestClient> CreateAzureRestClient(ArgumentSyntax syntax)
        {
            var azSettings = await UserSettings.GetAzureSettings();
            var tenantId = syntax.GetOption<string>(AzureTenantIdOption)
                ?? azSettings.TenantId;
            var clientId = syntax.GetOption<string>(AzureClientIdOption)
                ?? azSettings.ClientId;
            var secret = syntax.GetOption<string>(AzureSecretOption)
                ?? azSettings.ClientSecret;
            var subscriptionId = syntax.GetOption<string>(AzureSubscriptionIdOption)
                ?? azSettings.SubscriptionId;

            ValidateOption(tenantId, AzureTenantIdOption);
            ValidateOption(clientId, AzureClientIdOption);
            ValidateOption(secret, AzureSecretOption);
            ValidateOption(subscriptionId, AzureSubscriptionIdOption);

            var loginInfo = new ServicePrincipalLoginInformation
            {
                ClientId = clientId,
                ClientSecret = secret,
            };

            var credentials = new AzureCredentials(
                loginInfo, tenantId, AzureEnvironment.AzureGlobalCloud)
                .WithDefaultSubscription(subscriptionId);

            var builder = RestClient.Configure();
            var resClient = builder.WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithCredentials(credentials)
                .Build();
            return resClient;
        }

        protected static ArgumentSyntax DefineAzureOptions(ArgumentSyntax syntax)
        {
            return syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineOption(AzureTenantIdOption, help: Strings.HelpAzureTenantId)
                .DefineOption(AzureClientIdOption, help: Strings.HelpAzureClientId)
                .DefineOption(AzureSecretOption, help: Strings.HelpAzureSecret)
                .DefineOption(AzureSubscriptionIdOption, help: Strings.HelpAzureSubscriptionId)
                .DefineOption(AzureResourceGroupOption, help: Strings.HelpAzureResourceGroup);
        }

        private void ValidateOption(string value, string optionName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new CertesCliException(string.Format(Strings.ErrorOptionMissing, optionName));
            }
        }
    }
}

using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Certes.Cli.Commands
{
    internal abstract class AzureCommandBase : CommandBase
    {
        protected const string AzureResourceGroupOption = "--resource-group";

        public static string AzureTenantIdOption => "--tenant-id";
        public static string AzureClientIdOption => "--client-id";
        public static string AzureSecretOption => "--client-secret";
        public static string AzureSubscriptionIdOption => "--subscription-id";

        protected AzureCommandBase(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        protected async Task<RestClient> CreateAzureRestClient(AzureSettings options)
        {
            var azSettings = await UserSettings.GetAzureSettings();
            var tenantId = options.TenantId ?? azSettings.TenantId;
            var clientId = options.ClientId ?? azSettings.ClientId;
            var secret = options.ClientSecret ?? azSettings.ClientSecret;
            var subscriptionId = options.SubscriptionId ?? azSettings.SubscriptionId;

            ValidateOption(tenantId, AzureTenantIdOption);
            ValidateOption(clientId, AzureClientIdOption);
            ValidateOption(secret, AzureSecretOption);
            ValidateOption(subscriptionId, AzureSubscriptionIdOption);

            var loginInfo = new ServicePrincipalLoginInformation
            {
                ClientId = clientId,
                ClientSecret = secret,
            };

            return CreateRestClient(tenantId, subscriptionId, loginInfo);
        }

        protected static Command AddCommonOptions(Command command)
        {
            command.AddOption(new Option<Uri>(new[] { "--server", "-s" }, Strings.HelpServer));
            command.AddOption(new Option<string>(new[] { "--key-path", "--key", "-k" }, Strings.HelpKey));
            command.AddOption(new Option<string>(AzureTenantIdOption, Strings.HelpAzureTenantId));
            command.AddOption(new Option<string>(AzureClientIdOption, Strings.HelpAzureClientId));
            command.AddOption(new Option<string>(AzureSecretOption, Strings.HelpAzureSecret));
            command.AddOption(new Option<string>(AzureSubscriptionIdOption, Strings.HelpAzureSubscriptionId));
            command.AddOption(new Option<string>(AzureResourceGroupOption, Strings.HelpAzureResourceGroup));

            return command;
        }

        private static RestClient CreateRestClient(string tenantId, string subscriptionId, ServicePrincipalLoginInformation loginInfo)
        {
            var credentials = new AzureCredentials(
                loginInfo, tenantId, AzureEnvironment.AzureGlobalCloud)
                .WithDefaultSubscription(subscriptionId);

            var builder = RestClient.Configure();
            var resClient = builder.WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithCredentials(credentials)
                .Build();
            return resClient;
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

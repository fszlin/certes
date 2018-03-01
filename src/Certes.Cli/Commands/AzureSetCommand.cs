using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;
using static Certes.Cli.Commands.AzureCommand;

namespace Certes.Cli.Commands
{
    internal class AzureSetCommand : ICliCommand
    {
        private const string CommandText = "set";
        private const string ParamServer = "server";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(AzureSetCommand));

        private readonly IAcmeContextFactory contextFactory;
        private readonly IUserSettings userSettings;

        public CommandGroup Group => CommandGroup.Azure;

        public AzureSetCommand(IUserSettings userSettings, IAcmeContextFactory contextFactory)
        {
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var talentId = syntax.GetOption<string>(AzureTalentIdOption);
            var clientId = syntax.GetOption<string>(AzureClientIdOption);
            var secret = syntax.GetOption<string>(AzureSecretOption);
            var subscriptionId = syntax.GetOption<string>(AzureSubscriptionIdOption);

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
    }
}

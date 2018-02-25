using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AccountNewCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "new";
        private const string EmailParam = "email";
        private const string OutOption = "out";

        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountNewCommand));

        public CommandGroup Group { get; } = CommandGroup.Account;
        
        public AccountNewCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandAccountNew);
            
            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineOption(OutOption, help: Strings.HelpKeyOut)
                .DefineParameter(EmailParam, help: Strings.HelpEmail);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var acct = await ReadAccountKey(syntax);

            logger.Debug("Creating new account on '{0}'.", acct.Server);
            var key = acct.Key ?? KeyFactory.NewKey(KeyAlgorithm.ES256);
            var email = syntax.GetParameter<string>(EmailParam, true);

            var acme = ContextFactory.Create(acct.Server, key);
            var acctCtx = await acme.NewAccount(email, true);

            var outPath = syntax.GetOption<string>(OutOption);
            if (!string.IsNullOrWhiteSpace(outPath))
            {
                logger.Debug("Saving new account key to '{0}'.", outPath);
                var pem = key.ToPem();
                await File.WriteAllText(outPath, pem);
            }
            else
            {
                logger.Debug("Saving new account key to user settings.");
                await UserSettings.SetAccountKey(acct.Server, key);
            }

            return new
            {
                location = acctCtx.Location,
                resource = await acctCtx.Resource()
            };
        }
    }
}

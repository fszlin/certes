using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AccountNewCommand : ICliCommand
    {
        private const string CommandText = "new";
        private const string EmailParam = "email";
        private const string OutOption = "out";

        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountNewCommand));
        private readonly IAcmeContextFactory contextFactory;
        private readonly IUserSettings userSettings;
        private readonly IFileUtil fileUtil;

        public CommandGroup Group { get; } = CommandGroup.Account;
        
        public AccountNewCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
        {
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
            this.fileUtil = fileUtil;
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandAccountNew);
            
            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineOption(OutOption)
                .DefineParameter(EmailParam);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var acct = await ReadAccountKey(syntax);

            logger.Debug("Creating new account on '{0}'.", acct.Server);
            var key = acct.Key ?? KeyFactory.NewKey(KeyAlgorithm.ES256);
            var email = syntax.GetParameter<string>(EmailParam);
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentSyntaxException(
                    string.Format(Strings.ErrorParameterMissing, EmailParam));
            }

            var acme = contextFactory.Create(acct.Server, key);
            var acctCtx = await acme.NewAccount(email, true);

            var outPath = syntax.GetOption<string>(OutOption);
            if (!string.IsNullOrWhiteSpace(outPath))
            {
                var pem = key.ToPem();
                await fileUtil.WriteAllTexts(outPath, pem);
            }
            else
            {
                await userSettings.SetAccountKey(acct.Server, key);
            }

            return new
            {
                location = acctCtx.Location,
                resource = await acctCtx.Resource()
            };
        }

        private async Task<(Uri Server, IKey Key)> ReadAccountKey(
            ArgumentSyntax syntax)
        {
            var serverUri = syntax.GetServerOption() ??
                await userSettings.GetDefaultServer();

            var keyPath = syntax.GetKeyOption();
            if (!string.IsNullOrWhiteSpace(keyPath))
            {
                logger.Debug("Load account key form '{0}'.", keyPath);
                var pem = await fileUtil.ReadAllTexts(keyPath);
                return (serverUri, KeyFactory.FromPem(pem));
            }
        
            return (serverUri, null);
        }
    }
}

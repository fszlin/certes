using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AccountSetCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "set";
        private const string KeyParam = "key";

        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountSetCommand));

        public CommandGroup Group { get; } = CommandGroup.Account;

        public AccountSetCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandAccountSet);

            syntax
                .DefineServerOption()
                .DefineParameter(KeyParam, help: Strings.HelpKey);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, _) = await ReadAccountKey(syntax, false);

            var keyPath = syntax.GetParameter<string>(KeyParam, true);
            var pem = await File.ReadAllText(keyPath);
            var key = KeyFactory.FromPem(pem);

            logger.Debug("Setting account for '{0}'.", serverUri);

            var acme = ContextFactory.Invoke(serverUri, key);
            var acctCtx = await acme.Account();

            await UserSettings.SetAccountKey(serverUri, key);
            return new
            {
                location = acctCtx.Location,
                resource = await acctCtx.Resource()
            };
        }
    }
}

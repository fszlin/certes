using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AccountShowCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "show";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountNewCommand));

        public CommandGroup Group => CommandGroup.Account;

        public AccountShowCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandAccountShow);

            syntax
                .DefineServerOption()
                .DefineKeyOption();

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, true);

            logger.Debug("Loading account from '{0}'.", serverUri);

            var acme = ContextFactory.Create(serverUri, key);
            var acctCtx = await acme.Account();

            return new
            {
                location = acctCtx.Location,
                resource = await acctCtx.Resource()
            };
        }
    }
}

using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AccountUpdateCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "update";
        private const string EmailParam = "email";

        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountUpdateCommand));

        public CommandGroup Group { get; } = CommandGroup.Account;
        
        public AccountUpdateCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandAccountUpdate);

            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineParameter(EmailParam, help: Strings.HelpEmail);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, true);
            var email = syntax.GetParameter<string>(EmailParam, true);

            logger.Debug("Updating account on '{0}'.", serverUri);

            var acme = ContextFactory.Create(serverUri, key);
            var acctCtx = await acme.Account();
            var acct = await acctCtx.Update(new[] { $"mailto://{email}" }, true);

            return new
            {
                location = acctCtx.Location,
                resource = acct,
            };
        }
    }
}

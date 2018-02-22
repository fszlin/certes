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
        private readonly Func<Uri, IKey, IAcmeContext> contextFactory;
        private readonly ILogger logger = LogManager.GetLogger(nameof(ServerSetCommand));

        public CommandGroup Group { get; } = CommandGroup.Account;
        public IUserSettings Settings { get; private set; }

        public AccountNewCommand(IUserSettings userSettings)
            : this(userSettings, null)
        {
        }

        public AccountNewCommand(IUserSettings userSettings, Func<Uri, IKey, IAcmeContext> contextFactory)
        {
            Settings = userSettings;
            this.contextFactory = contextFactory ?? ContextFactory.Create;
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandAccountNew);
            syntax.DefineServerOption()
                .DefineKeyOption();
            return cmd;
        }

        public Task<object> Execute(ArgumentSyntax syntax)
        {
            throw new NotImplementedException();
        }
    }

}

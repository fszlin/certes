using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class ServerShowCommand : ICliCommand
    {
        private const string ParamServer = "server";
        private readonly Func<Uri, IKey, IAcmeContext> contextFactory;
        private readonly ILogger logger = LogManager.GetLogger(nameof(ServerSetCommand));

        public CommandGroup Group { get; } = CommandGroup.Server;
        public IUserSettings Settings { get; private set; }

        public ServerShowCommand(IUserSettings userSettings)
            : this(userSettings, null)
        {
        }

        public ServerShowCommand(IUserSettings userSettings, Func<Uri, IKey, IAcmeContext> contextFactory)
        {
            Settings = userSettings;
            this.contextFactory = contextFactory ?? ContextFactory.Create;
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand("show", help: Strings.HelpCommandServerShow);
            syntax.DefineServerOption();

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var serverUri = syntax.GetServerOption() ??
                await Settings.GetDefaultServer();

            var ctx = contextFactory(serverUri, null);
            logger.Debug("Loading directory from '{0}'", serverUri);
            var directory = await ctx.GetDirectory();

            return new
            {
                location = serverUri,
                resource = directory,
            };
        }
    }

}

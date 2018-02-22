using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    // e.x: server set --server https://acme-staging-v02.api.letsencrypt.org/directory
    internal class ServerSetCommand : ICliCommand
    {
        private const string CommandText = "set";
        private const string ParamServer = "server";
        private readonly ILogger logger = LogManager.GetLogger(nameof(ServerSetCommand));

        private readonly Func<Uri, IKey, IAcmeContext> contextFactory;

        public CommandGroup Group { get; } = CommandGroup.Server;
        public IUserSettings Settings { get; private set; }

        public ServerSetCommand(IUserSettings userSettings)
            : this(userSettings, null)
        {
        }

        public ServerSetCommand(IUserSettings userSettings, Func<Uri, IKey, IAcmeContext> contextFactory)
        {
            Settings = userSettings;
            this.contextFactory = contextFactory ?? ContextFactory.Create;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var serverUriParam = syntax.GetActiveOptions()
                .Where(p => p.Name == ParamServer)
                .OfType<Argument<Uri>>()
                .First();

            if (!serverUriParam.IsSpecified)
            {
                syntax.ReportError(string.Format(Strings.OptionMissing, ParamServer));
            }

            var ctx = contextFactory(serverUriParam.Value, null);
            logger.Debug("Loading directory from '{0}'", serverUriParam.Value);
            var directory = await ctx.GetDirectory();
            await Settings.SetDefaultServer(serverUriParam.Value);
            return new
            {
                location = serverUriParam.Value,
                directory,
            };
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandServerSet);
            syntax.DefineServerOption(true);

            return cmd;
        }
    }
}

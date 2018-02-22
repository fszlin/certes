using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme;
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
            syntax.DefineOption(
                ParamServer, WellKnownServers.LetsEncryptV2, false, Strings.HelpOptionServer);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var serverUriParam = syntax.GetActiveOptions()
                .Where(p => p.Name == ParamServer)
                .OfType<Argument<Uri>>()
                .First();

            var ctx = contextFactory(serverUriParam.Value, null);
            logger.Debug("Loading directory from '{0}'", serverUriParam.Value);
            var directory = await ctx.GetDirectory();

            return new
            {
                location = serverUriParam.Value,
                directory,
            };
        }
    }

}

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
        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountNewCommand));

        private readonly IAcmeContextFactory contextFactory;
        private readonly IUserSettings userSettings;

        public CommandGroup Group { get; } = CommandGroup.Server;

        public ServerSetCommand(IUserSettings userSettings, IAcmeContextFactory contextFactory)
        {
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var serverUriParam = syntax.GetActiveOptions()
                .Where(p => p.Name == ParamServer)
                .OfType<Argument<Uri>>()
                .First();

            if (!serverUriParam.IsSpecified)
            {
                syntax.ReportError(string.Format(Strings.ErrorOptionMissing, ParamServer));
            }

            var ctx = contextFactory.Create(serverUriParam.Value, null);
            logger.Debug("Loading directory from '{0}'", serverUriParam.Value);
            var directory = await ctx.GetDirectory();
            await userSettings.SetDefaultServer(serverUriParam.Value);
            return new
            {
                location = serverUriParam.Value,
                resource = directory,
            };
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandServerSet);
            syntax.DefineServerOption();

            return cmd;
        }
    }
}

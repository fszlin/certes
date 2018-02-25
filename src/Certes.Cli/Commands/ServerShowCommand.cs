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
        private static readonly ILogger logger = LogManager.GetLogger(nameof(ServerShowCommand));

        private readonly IAcmeContextFactory contextFactory;
        private readonly IUserSettings userSettings;
        public CommandGroup Group { get; } = CommandGroup.Server;

        public ServerShowCommand(IUserSettings userSettings, IAcmeContextFactory contextFactory)
        {
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
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
                await userSettings.GetDefaultServer();

            var ctx = contextFactory.Create(serverUri, null);
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

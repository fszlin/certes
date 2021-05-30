using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class ServerShowCommand : ICliCommand
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(ServerShowCommand));
        private readonly AcmeContextFactory contextFactory;
        private readonly IUserSettings userSettings;

        public ServerShowCommand(IUserSettings userSettings, AcmeContextFactory contextFactory)
        {
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
        }

        public CommandGroup Group => CommandGroup.Server;

        public Command Define()
        {
            var cmd = new Command("show", Strings.HelpCommandServerShow)
            {
                new Option(new[]{ "--server", "-s" }, Strings.HelpServer),
            };

            cmd.Handler = CommandHandler.Create(async (Uri server, IConsole console) =>
            {
                var serverUri = server ?? await userSettings.GetDefaultServer();

                var ctx = contextFactory.Invoke(serverUri, null);
                logger.Debug("Loading directory from '{0}'", serverUri);
                var directory = await ctx.GetDirectory();

                console.WriteAsJson(new
                {
                    location = serverUri,
                    resource = directory,
                });
            });

            return cmd;
        }
    }
}

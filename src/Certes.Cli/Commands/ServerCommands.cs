using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class ServerCommands : ICliCommandFactory
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(ServerCommands));
        private readonly AcmeContextFactory contextFactory;
        private readonly IUserSettings userSettings;

        public ServerCommands(IUserSettings userSettings, AcmeContextFactory contextFactory)
        {
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
        }

        public Command Create()
        {
            var cmd = new Command(CommandGroup.Server.Command, CommandGroup.Server.Help);

            cmd.AddCommand(CreateSetCommand());
            cmd.AddCommand(CreateShowCommand());

            return cmd;
        }

        private Command CreateSetCommand()
        {
            var newServerOption = new Option<Uri>("--new-server", Strings.HelpNewServer);

            // For backward compatibility 
            newServerOption.AddAlias("--server-uri");
            newServerOption.IsRequired = true;

            var cmd = new Command("set", Strings.HelpCommandServerSet)
            {
                newServerOption,
            };

            cmd.Handler = CommandHandler.Create(async (Uri newServer, IConsole console) =>
            {
                var ctx = contextFactory.Invoke(newServer, null);
                logger.Debug("Loading directory from '{0}'", newServer);
                var directory = await ctx.GetDirectory();
                await userSettings.SetDefaultServer(newServer);

                console.WriteAsJson(new
                {
                    location = newServer,
                    resource = directory,
                });
            });

            return cmd;
        }

        private Command CreateShowCommand()
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

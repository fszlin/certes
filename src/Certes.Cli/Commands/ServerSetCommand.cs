using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class ServerSetCommand : ICliCommand
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(ServerSetCommand));
        private readonly AcmeContextFactory contextFactory;
        private readonly IUserSettings userSettings;

        public ServerSetCommand(IUserSettings userSettings, AcmeContextFactory contextFactory)
        {
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
        }

        public CommandGroup Group => CommandGroup.Server;

        public Command Define()
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
    }
}

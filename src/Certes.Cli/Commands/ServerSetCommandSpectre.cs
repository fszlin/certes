using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Certes.Json;
using NLog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Certes.Cli.Commands
{
    /// <summary>
    /// Spectre.Console.Cli based implementation of the Server Set command.
    /// This demonstrates the migration pattern from System.CommandLine to Spectre.Console.Cli.
    /// The command extracts its logic into a shared Execute method that both parsers can use.
    /// 
    /// **Migration Pattern**:
    /// 1. Define command-specific settings class (SpectreSettings)
    /// 2. Implement Execute method with parser-agnostic logic
    /// 3. Register as a Spectre command (alongside or replacing System.CommandLine version)
    /// </summary>
    internal class ServerSetCommandSpectre : Command<ServerSetCommandSpectre.Settings>
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(ServerSetCommandSpectre));
        private readonly AcmeContextFactory contextFactory;
        private readonly IUserSettings userSettings;

        public class Settings : CommandSettings
        {
            [CommandArgument(0, "[NEW-SERVER]")]
            public Uri NewServer { get; init; }
        }

        public ServerSetCommandSpectre(IUserSettings userSettings, AcmeContextFactory contextFactory)
        {
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
        }

        public override int Execute(CommandContext context, Settings settings)
        {
            try
            {
                var task = ExecuteCore(settings.NewServer);
                task.Wait();
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]{0}[/]", ex.Message);
                return 1;
            }
        }

        /// <summary>
        /// Core logic extracted from the original System.CommandLine command.
        /// This method is parser-agnostic and can be called by both implementations.
        /// </summary>
        private async Task ExecuteCore(Uri newServer)
        {
            var ctx = contextFactory.Invoke(newServer, null);
            logger.Debug("Loading directory from '{0}'", newServer);
            var directory = await ctx.GetDirectory();
            await userSettings.SetDefaultServer(newServer);

            var output = new
            {
                location = newServer,
                resource = directory,
            };

            AnsiConsole.Write(System.Text.Json.JsonSerializer.Serialize(output, JsonUtil.CreateSettings()));
        }
    }
}

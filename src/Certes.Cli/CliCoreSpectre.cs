using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Certes.Cli.Commands;
using Certes.Cli.Settings;
using Certes.Json;
using NLog;
using Spectre.Console.Cli;

namespace Certes.Cli
{
    /// <summary>
    /// Spectre.Console.Cli-based CLI implementation.
    /// Spectre handles top-level command parsing and dispatch.
    /// Existing System.CommandLine command handlers are invoked through a
    /// compatibility bridge to preserve current behavior.
    /// </summary>
    internal class CliCoreSpectre
    {
        private readonly ILogger consoleLogger = LogManager.GetLogger(nameof(CliCoreSpectre));
        private static readonly JsonSerializerOptions jsonSerializerSettings = JsonUtil.CreateSettings();
        private readonly IEnumerable<ICliCommand> commands;
        private readonly IUserSettings userSettings;
        private readonly AcmeContextFactory contextFactory;

        public CliCoreSpectre(
            IEnumerable<ICliCommand> commands,
            IUserSettings userSettings,
            AcmeContextFactory contextFactory)
        {
            this.commands = commands;
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
        }

        /// <summary>
        /// Runs the CLI using Spectre for parsing and dispatch.
        /// </summary>
        public async Task<bool> Run(string[] args)
        {
            try
            {
                var rootCommand = BuildRootCommand();
                Func<string[], Task<int>> legacyDispatch = relayArgs => rootCommand.InvokeAsync(relayArgs);

                if (ShouldBypassToLegacy(args))
                {
                    return await legacyDispatch(args) == 0;
                }

                var app = new CommandApp();
                app.Configure(config =>
                {
                    config.SetApplicationName("certes");

                    config.Settings.StrictParsing = false;
                    config.Settings.ConvertFlagsToRemainingArguments = true;

                    ConfigureServerBranch(config);
                    ConfigureRelayBranch(config, legacyDispatch, CommandGroup.Account);
                    ConfigureRelayBranch(config, legacyDispatch, CommandGroup.Order);
                    ConfigureRelayBranch(config, legacyDispatch, CommandGroup.Certificate);
                    ConfigureRelayBranch(config, legacyDispatch, CommandGroup.Azure);
                });

                var result = app.Run(args);
                return result == 0;
            }
            catch (Exception ex)
            {
                consoleLogger.Error(ex.Message);
                consoleLogger.Debug(ex);
                return false;
            }
        }

        private static bool ShouldBypassToLegacy(string[] args)
        {
            if (args == null || args.Length < 2)
            {
                return false;
            }

            var isHelp = args[1] == "-h" || args[1] == "--help";
            if (!isHelp)
            {
                return false;
            }

            return args[0] == CommandGroup.Account.Command ||
                args[0] == CommandGroup.Order.Command ||
                args[0] == CommandGroup.Certificate.Command ||
                args[0] == CommandGroup.Azure.Command;
        }

        private void ConfigureServerBranch(IConfigurator config)
        {
            config.AddBranch(CommandGroup.Server.Command, branch =>
            {
                branch.AddAsyncDelegate<ServerSetSettings>("set", async (_, settings) =>
                {
                    var ctx = contextFactory.Invoke(settings.NewServer, null);
                    consoleLogger.Debug("Loading directory from '{0}'", settings.NewServer);
                    var directory = await ctx.GetDirectory();
                    await userSettings.SetDefaultServer(settings.NewServer);

                    WriteJson(new
                    {
                        location = settings.NewServer,
                        resource = directory,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandServerSet);

                branch.AddAsyncDelegate<ServerShowSettings>("show", async (_, settings) =>
                {
                    var serverUri = settings.Server ?? await userSettings.GetDefaultServer();
                    var ctx = contextFactory.Invoke(serverUri, null);
                    consoleLogger.Debug("Loading directory from '{0}'", serverUri);
                    var directory = await ctx.GetDirectory();

                    WriteJson(new
                    {
                        location = serverUri,
                        resource = directory,
                    });

                    return 0;
                }).WithDescription(Strings.HelpCommandServerShow);
            });
        }

        private static void WriteJson(object value)
        {
            Console.WriteLine(JsonSerializer.Serialize(value, jsonSerializerSettings));
        }

        private void ConfigureRelayBranch(IConfigurator config, Func<string[], Task<int>> legacyDispatch, CommandGroup group)
        {
            var commandEntries = commands
                .Where(c => c.Group == group)
                .Select(c => new { Name = c.GetCommandName(), Description = c.GetCommandDescription() })
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            config.AddBranch(group.Command, branch =>
            {
                foreach (var entry in commandEntries)
                {
                    branch.AddDelegate(entry.Name, context =>
                    {
                        var forwarded = (context.Arguments ?? Array.Empty<string>()).ToArray();
                        return legacyDispatch(forwarded).GetAwaiter().GetResult();
                    })
                    .WithDescription(entry.Description);
                }
            });
        }

        /// <summary>
        /// Builds the System.CommandLine root command hierarchy.
        /// During migration phase, this remains the execution path.
        /// Once commands are migrated to Spectre.Console.Cli.Command types,
        /// this will be replaced with a full CommandApp implementation.
        /// </summary>
        private RootCommand BuildRootCommand()
        {
            var rootCommand = new RootCommand();

            foreach (var commandGroup in commands.GroupBy(c => c.Group))
            {
                var groupCmd = new System.CommandLine.Command(commandGroup.Key.Command, commandGroup.Key.Help);
                foreach (var cmd in commandGroup)
                {
                    groupCmd.AddCommand(cmd.Define());
                }

                rootCommand.Add(groupCmd);
            }

            return rootCommand;
        }

        /// <summary>
        /// Gets the registered command metadata for documentation purposes.
        /// </summary>
        public IEnumerable<CommandMetadata> GetCommandMetadata()
        {
            return commands.Select(cmd => new CommandMetadata
            {
                Group = cmd.Group.Command,
                Name = cmd.GetCommandName(),
                Description = cmd.GetCommandDescription()
            });
        }
    }

    /// <summary>
    /// Metadata about a registered command, used for documentation and help.
    /// </summary>
    internal class CommandMetadata
    {
        public string Group { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    internal sealed class ServerSetSettings : CommandSettings
    {
        [CommandArgument(0, "<NEW_SERVER>")]
        public Uri NewServer { get; init; }
    }

    internal sealed class ServerShowSettings : CommandSettings
    {
        [CommandOption("-s|--server <SERVER>")]
        public Uri Server { get; init; }
    }

}

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Commands;
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
        private readonly IEnumerable<ICliCommand> commands;

        public CliCoreSpectre(IEnumerable<ICliCommand> commands)
        {
            this.commands = commands;
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

                var app = new CommandApp();
                app.Configure(config =>
                {
                    config.SetApplicationName("certes");

                    config.Settings.StrictParsing = false;
                    config.Settings.ConvertFlagsToRemainingArguments = true;

                    ConfigureRelayBranch(config, legacyDispatch, CommandGroup.Server);
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

        private void ConfigureRelayBranch(IConfigurator config, Func<string[], Task<int>> legacyDispatch, CommandGroup group)
        {
            config.AddBranch(group.Command, branch =>
            {
                foreach (var commandName in commands
                    .Where(c => c.Group == group)
                    .Select(c => c.GetCommandName())
                    .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    branch.AddDelegate(commandName, context =>
                    {
                        var forwarded = (context.Arguments ?? Array.Empty<string>()).ToArray();
                        return legacyDispatch(forwarded).GetAwaiter().GetResult();
                    });
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

}

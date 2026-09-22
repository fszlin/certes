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
                SpectreRelayCommand.DispatchAsync = relayArgs => rootCommand.InvokeAsync(relayArgs);

                var app = new CommandApp();
                app.Configure(config =>
                {
                    config.SetApplicationName("certes");

                    config.AddCommand<ServerRelayCommand>("server")
                        .WithDescription(CommandGroup.Server.Help);
                    config.AddCommand<AccountRelayCommand>("account")
                        .WithDescription(CommandGroup.Account.Help);
                    config.AddCommand<OrderRelayCommand>("order")
                        .WithDescription(CommandGroup.Order.Help);
                    config.AddCommand<CertificateRelayCommand>("cert")
                        .WithDescription(CommandGroup.Certificate.Help);
                    config.AddCommand<AzureRelayCommand>("az")
                        .WithDescription(CommandGroup.Azure.Help);
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

    internal class SpectreCompatibilitySettings : CommandSettings
    {
        [CommandArgument(0, "[ARGS]")]
        public string[] Args { get; init; }
    }

    internal abstract class SpectreRelayCommand : Command<SpectreCompatibilitySettings>
    {
        public static Func<string[], Task<int>> DispatchAsync { get; set; }

        protected abstract string GroupName { get; }

        public override int Execute(CommandContext context, SpectreCompatibilitySettings settings)
        {
            var suffix = settings.Args ?? Array.Empty<string>();
            var args = new[] { GroupName }.Concat(suffix).ToArray();
            var exitCode = DispatchAsync(args).GetAwaiter().GetResult();
            return exitCode;
        }
    }

    internal sealed class ServerRelayCommand : SpectreRelayCommand
    {
        protected override string GroupName => CommandGroup.Server.Command;
    }

    internal sealed class AccountRelayCommand : SpectreRelayCommand
    {
        protected override string GroupName => CommandGroup.Account.Command;
    }

    internal sealed class OrderRelayCommand : SpectreRelayCommand
    {
        protected override string GroupName => CommandGroup.Order.Command;
    }

    internal sealed class CertificateRelayCommand : SpectreRelayCommand
    {
        protected override string GroupName => CommandGroup.Certificate.Command;
    }

    internal sealed class AzureRelayCommand : SpectreRelayCommand
    {
        protected override string GroupName => CommandGroup.Azure.Command;
    }
}

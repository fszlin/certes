using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Commands;
using Certes.Json;
using Newtonsoft.Json;
using NLog;

namespace Certes.Cli
{
    internal class CliCore
    {
        private readonly ILogger consoleLogger = LogManager.GetLogger(nameof(CliCore));
        private readonly JsonSerializerSettings jsonSettings = JsonUtil.CreateSettings();
        private readonly IEnumerable<ICliCommand> commands;

        public CliCore(IEnumerable<ICliCommand> commands)
        {
            this.commands = commands;
        }

        public async Task<bool> Run(string[] args)
        {
            try
            {
                var cmd = MatchCommand(args);
                if (cmd == null)
                {
                    return false;
                }

                var result = await cmd.Value.Command.Execute(cmd.Value.Syntax);
                consoleLogger.Info(JsonConvert.SerializeObject(
                    result, Formatting.Indented, jsonSettings));
                return true;
            }
            catch (Exception ex)
            {
                consoleLogger.Error(ex.Message);
                consoleLogger.Debug(ex);
                return false;
            }
        }

        private (ICliCommand Command, ArgumentSyntax Syntax)? MatchCommand(string[] args)
        {
            var commandGroups = commands.ToLookup(c => c.Group);

            var group = MatchCommandGroup(args, commandGroups);
            if (group == null)
            {
                return null;
            }

            return MatchCommand(args, group.Value.Commands, group.Value.Syntax);
        }

        private (ICliCommand Command, ArgumentSyntax Syntax)? MatchCommand(
            string[] args, IEnumerable<ICliCommand> groupCommands, ArgumentSyntax groupSyntax)
        {
            string helpText = null;
            try
            {
                var isHelpRequested = IsHelpRequested(args);
                ICliCommand matchCommand = null;
                var cmdSyntax = ArgumentSyntax.Parse(args.Skip(1).ToArray(), s =>
                {
                    s.HandleErrors = false;
                    s.HandleHelp = false;
                    s.ErrorOnUnexpectedArguments = false;
                    s.ApplicationName = $"{groupSyntax.ApplicationName} {groupSyntax.ActiveCommand}";
                    foreach (var cmd in groupCommands)
                    {
                        var arg = cmd.Define(s);
                        if (arg.IsActive)
                        {
                            matchCommand = cmd;
                        }
                    }

                    if (isHelpRequested)
                    {
                        helpText = s.GetHelpText();
                    }
                });

                if (!isHelpRequested)
                {
                    return (matchCommand, cmdSyntax);
                }
            }
            catch (ArgumentSyntaxException)
            {
                if (!IsHelpRequested(args))
                {
                    throw;
                }
            }

            consoleLogger.Info(helpText);
            return null;
        }

        private (IEnumerable<ICliCommand> Commands, ArgumentSyntax Syntax)? MatchCommandGroup(
            string[] args, ILookup<CommandGroup, ICliCommand> commandGroups)
        {
            string helpText = null;
            var isHelpRequested = IsHelpRequested(args);
            try
            {
                IEnumerable<ICliCommand> commands = null;
                var groupSyntax = ArgumentSyntax.Parse(args, s =>
                {
                    s.HandleErrors = false;
                    s.HandleHelp = false;
                    s.ErrorOnUnexpectedArguments = false;
                    foreach (var cmdGroup in commandGroups)
                    {
                        var cmd = s.DefineCommand(cmdGroup.Key.Command, help: cmdGroup.Key.Help);
                        if (cmd.IsActive)
                        {
                            commands = cmdGroup;
                        }
                    }

                    if (isHelpRequested)
                    {
                        helpText = s.GetHelpText();
                    }
                });

                return (commands, groupSyntax);
            }
            catch (ArgumentSyntaxException)
            {
                if (isHelpRequested)
                {
                    consoleLogger.Info(helpText);
                    return null;
                }

                throw;
            }
        }

        private static bool IsHelpRequested(string[] args)
            => args.Intersect(new[] { "-?", "-h", "--help" }).Any();
    }
}

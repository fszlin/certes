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

        private readonly RootCommand rootCommand;

        public CliCore(IEnumerable<ICliCommand> commands)
        {
            rootCommand = new RootCommand();

            foreach (var commandGroup in commands.GroupBy(c => c.Group))
            {
                var groupCmd = new Command(commandGroup.Key.Command, commandGroup.Key.Help);
                foreach (var cmd in commandGroup)
                {
                    groupCmd.AddCommand(cmd.Define());
                }

                rootCommand.Add(groupCmd);
            }
        }

        public async Task<bool> Run(string[] args)
        {
            try
            {
                await rootCommand.InvokeAsync(args);
                return true;
            }
            catch (Exception ex)
            {
                consoleLogger.Error(ex.Message);
                consoleLogger.Debug(ex);
                return false;
            }
        }
    }
}

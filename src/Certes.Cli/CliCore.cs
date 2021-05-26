using System;
using System.Collections.Generic;
using System.CommandLine;
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

        public CliCore(IEnumerable<ICliCommandFactory> commands)
        {
            rootCommand = new RootCommand();
            foreach (var cmd in commands)
            {
                rootCommand.Add(cmd.Create());
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

using System;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Certes.Cli
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            ConfigureConsoleLogger();

            var processor = new CliCore();
            var succeed = await processor.Run(args);
            return succeed ? 0 : 1;
        }

        private static void ConfigureConsoleLogger()
        {
            var config = new LoggingConfiguration();

            if (HasFlags("CERTES_DEBUG"))
            {
                config.LoggingRules.Add(
                    new LoggingRule("*", LogLevel.Debug, new ColoredConsoleTarget
                    {
                        Layout = "${message}${onexception:${newline}${exception:format=tostring}}",
                    }));
            }
            else
            {
                var consoleRule = new LoggingRule("*", LogLevel.Info, new ColoredConsoleTarget
                {
                    Layout = "${message}",
                });
                config.LoggingRules.Add(consoleRule);
            }

            LogManager.Configuration = config;
        }

        private static bool HasFlags(string environmentVariableName)
            => string.Equals("true", Environment.GetEnvironmentVariable(environmentVariableName), StringComparison.OrdinalIgnoreCase);
    }
}

using System;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Certes.Cli
{
    public class Program
    {
        internal const string ConsoleLoggerName = "certes-cli-console-logger";

        public static async Task<int> Main(string[] args)
        {
            ConfigureConsoleLogger(
                string.Equals("true", Environment.GetEnvironmentVariable("CERTES_DEBUG"), StringComparison.OrdinalIgnoreCase));

            var succeed = await new CliCore().Process(args);
            return succeed ? 0 : 1;
        }

        private static void ConfigureConsoleLogger(bool verbose)
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget
            {
                Layout = @"${message}${onexception:${newline}${exception:format=tostring}}"
            };

            config.AddTarget(ConsoleLoggerName, consoleTarget);

            var consoleRule = new LoggingRule("*", LogLevel.Info, consoleTarget);
            config.LoggingRules.Add(consoleRule);

            if (verbose)
            {
                config.LoggingRules.Add(
                    new LoggingRule("*", LogLevel.Debug, LogLevel.Debug, new ColoredConsoleTarget
                    {
                        Layout = "${time} ${message}${onexception:${newline}${exception:format=tostring}}",
                    }));
            }

            LogManager.Configuration = config;
        }
    }
}

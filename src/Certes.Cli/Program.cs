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
            using (var factory = new LogFactory(ConfigureConsoleLogger()))
            {
                var logger = factory.GetLogger(ConsoleLoggerName);
                var succeed = await new CliV1(logger).Process(args);
                return succeed ? 0 : 1;
            }
        }

        private static LoggingConfiguration ConfigureConsoleLogger()
        {
            var config = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget
            {
                Layout = @"${message}"
            };

            config.AddTarget(ConsoleLoggerName, consoleTarget);

            var consoleRule = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            config.LoggingRules.Add(consoleRule);

            return config;
        }
    }
}

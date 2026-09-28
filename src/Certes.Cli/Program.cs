using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Certes.Cli
{
    public class Program
    {
        internal static async Task<int> Main(string[] args)
        {
            ConfigureConsoleLogger();
            using var container = ConfigureContainer();
            using var cancellation = new CancellationTokenSource();
            var interruptCount = 0;
            ConsoleCancelEventHandler cancel = (_, e) =>
            {
                e.Cancel = CancelInvocation(cancellation, ref interruptCount);
            };
            Console.CancelKeyPress += cancel;
            try
            {
                return await container.Resolve<CliCoreSpectre>().RunWithExitCode(args, cancellation.Token);
            }
            finally
            {
                Console.CancelKeyPress -= cancel;
            }
        }

        internal static IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();
            builder
                .RegisterAssemblyTypes(typeof(Program).GetTypeInfo().Assembly)
                .AsImplementedInterfaces();
            builder.RegisterType<CliCoreSpectre>();
            builder.RegisterType<AcmeContext>().As<IAcmeContext>();

            return builder.Build();
        }

        internal static bool CancelInvocation(CancellationTokenSource cancellation, ref int interruptCount)
        {
            // A second interrupt restores the console's default termination.
            if (Interlocked.Increment(ref interruptCount) > 1) return false;
            cancellation.Cancel();
            return true;
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

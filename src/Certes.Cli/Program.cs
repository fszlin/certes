using System;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
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
            var container = ConfigureContainer();

            var succeed = await container.Resolve<CliCore>().Run(args);
            return succeed ? 0 : 1;
        }

        internal static IContainer ConfigureContainer()
        {
            var builder = new ContainerBuilder();
            builder
                .RegisterAssemblyTypes(typeof(CliCore).GetTypeInfo().Assembly)
                .AsImplementedInterfaces();
            builder.RegisterType<CliCore>();
            builder.RegisterType<DnsManagementClient>().As<IDnsManagementClient>();
            builder.RegisterType<ResourceManagementClient>().As<IResourceManagementClient>();
            builder.RegisterType<WebSiteManagementClient>().As<IWebSiteManagementClient>();

            return builder.Build();
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

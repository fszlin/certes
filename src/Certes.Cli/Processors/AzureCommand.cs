using System;
using System.CommandLine;
using Certes.Cli.Options;
using NLog;

namespace Certes.Cli.Processors
{
    internal class AzureCommand
    {
        public ILogger Logger { get; } = LogManager.GetCurrentClassLogger();
        private AzureOptions Args { get; }

        public AzureCommand(AzureOptions args)
        {
            Args = args;
        }

        public static AzureOptions TryParse(ArgumentSyntax syntax)
        {
            var options = new AzureOptions();

            var command = Command.Undefined;
            syntax.DefineCommand("azure", ref command, Command.Azure, "Deploy to Azure.");
            if (command == Command.Undefined)
            {
                return null;
            }

            syntax.DefineOption("user", ref options.UserName, $"Azure user name or client ID.");
            syntax.DefineOption("pwd", ref options.Password, $"Azure password or client secret.");
            syntax.DefineOption("talent", ref options.Talent, s => new Guid(s), $"Azure talent ID.");
            syntax.DefineOption("subscription", ref options.Subscription, s => new Guid(s), $"Azure subscription ID.");
            syntax.DefineOption("order", ref options.OrderUri, s => new Uri(s), $"ACME order URI.");
            syntax.DefineOption("cloud", ref options.CloudEnvironment, a => (AuzreCloudEnvironment)Enum.Parse(typeof(AuzreCloudEnvironment), a?.Replace("-", ""), true), $"ACME order URI.");

            syntax.DefineOption("key", ref options.Path, $"File path to the account key to use.");
            syntax.DefineOption("verbose", ref options.Verbose, $"Print process log.");

            syntax.DefineParameter(
                "action",
                ref options.Action,
                a => (AzureAction)Enum.Parse(typeof(AzureAction), a?.Replace("-", ""), true),
                "Order action");
            syntax.DefineParameter("name", ref options.Value, "Domain name");

            return options;
        }

    }
}

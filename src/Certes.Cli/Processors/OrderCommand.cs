using System;
using System.CommandLine;
using Certes.Cli.Options;
using NLog;

namespace Certes.Cli.Processors
{
    internal class OrderCommand
    {
        public ILogger Logger { get; } = LogManager.GetCurrentClassLogger();
        private OrderOptions Args { get; }

        public OrderCommand(OrderOptions args)
        {
            Args = args;
        }

        public static OrderOptions TryParse(ArgumentSyntax syntax)
        {
            var options = new OrderOptions();

            var command = Command.Undefined;
            syntax.DefineCommand("order", ref command, Command.Account, "Manange ACME orders.");
            if (command == Command.Undefined)
            {
                return null;
            }

            syntax.DefineOption("server", ref options.Server, s => new Uri(s), $"ACME Directory Resource URI.");
            syntax.DefineOption("key", ref options.Path, $"File path to the account key to use.");
            syntax.DefineOption("force", ref options.Force, $"Overwrite exising account key.");
            syntax.DefineOption("verbose", ref options.Verbose, $"Print process log.");

            syntax.DefineParameter(
                "action",
                ref options.Action,
                a => (OrderAction)Enum.Parse(typeof(OrderAction), a?.Replace("-", ""), true),
                "Order action");

            return options;
        }
    }
}

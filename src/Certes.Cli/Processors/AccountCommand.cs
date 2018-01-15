using System;
using System.CommandLine;
using Certes.Cli.Options;

namespace Certes.Cli.Processors
{
    internal class AccountCommand
    {
        public static AccountOptions TryParse(ArgumentSyntax syntax)
        {
            var options = new AccountOptions();

            var command = Command.Undefined;
            syntax.DefineCommand("account", ref command, Command.Account, "Manange ACME account.");
            if (command == Command.Undefined)
            {
                return null;
            }

            syntax.DefineOption("email", ref options.Email, "Email used for registration and recovery contact. (default: None)");
            syntax.DefineOption("agree-tos", ref options.AgreeTos, $"Agree to the ACME Subscriber Agreement (default: {options.AgreeTos})");

            syntax.DefineOption("server", ref options.Server, s => new Uri(s), $"ACME Directory Resource URI.");
            syntax.DefineOption("p|path", ref options.Path, $"Path to the account key.");

            syntax.DefineParameter(
                "action",
                ref options.Action,
                a => (AccountAction)Enum.Parse(typeof(AccountAction), a, true),
                "Account action");

            return options;
        }
    }
}

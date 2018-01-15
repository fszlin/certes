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
            syntax.DefineOption("agree-tos", ref options.AgreeTos, $"Agree to the ACME Subscriber Agreement. (default: {options.AgreeTos})");

            syntax.DefineOption("server", ref options.Server, s => new Uri(s), $"ACME Directory Resource URI.");
            syntax.DefineOption("key", ref options.Path, $"File path to the account key to use.");

            syntax.DefineParameter(
                "action",
                ref options.Action,
                a => (AccountAction)Enum.Parse(typeof(AccountAction), a?.Replace("-", ""), true),
                "Account action");

            if (options.Action == AccountAction.New)
            {
                if (string.IsNullOrWhiteSpace(options.Email))
                {
                    syntax.ReportError("Please enter the admin email.");
                    return null;
                }
            }
            else if (options.Action == AccountAction.Update)
            {
                if (string.IsNullOrWhiteSpace(options.Email) && !options.AgreeTos)
                {
                    syntax.ReportError("Please enter the data to update.");
                    return null;
                }
            }
            else if (options.Action == AccountAction.Set)
            {
                if (string.IsNullOrWhiteSpace(options.Path))
                {
                    syntax.ReportError("Please enter the key file path.");
                    return null;
                }
            }

            return options;
        }
    }
}

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Options;
using Certes.Cli.Settings;
using NLog;
using ValidationFunc = System.Func<Certes.Cli.Options.AccountOptions, bool>;

namespace Certes.Cli.Processors
{
    internal class AccountCommand
    {
        private static readonly List<(AccountAction Action, ValidationFunc IsValid, string Help)> validations = new List<(AccountAction, ValidationFunc, string)>
        {
            (AccountAction.New, (ValidationFunc)(o => !string.IsNullOrWhiteSpace(o.Email)), "Please enter the admin email."),
            (AccountAction.Update, (ValidationFunc)(o => !string.IsNullOrWhiteSpace(o.Email) || o.AgreeTos), "Please enter the data to update."),
            (AccountAction.Set, (ValidationFunc)(o => !string.IsNullOrWhiteSpace(o.Path)), "Please enter the key file path."),
        };

        public ILogger Logger { get; } = LogManager.GetCurrentClassLogger();
        private AccountOptions Args { get; }
        public UserSettings Settings { get; private set; }

        public AccountCommand(AccountOptions args, UserSettings userSettings)
        {
            Args = args;
            Settings = userSettings;
        }

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

            syntax.DefineOption<Uri>("server", ref options.Server, $"ACME Directory Resource URI.");
            syntax.DefineOption("key", ref options.Path, $"File path to the account key to use.");
            syntax.DefineOption("force", ref options.Force, $"Overwrite exising account key.");

            syntax.DefineEnumParameter("action", ref options.Action, "Account action");

            foreach (var validation in validations)
            {
                if (options.Action == validation.Action && !validation.IsValid(options))
                {
                    syntax.ReportError(validation.Help);
                }
            }

            return options;
        }

        public async Task<object> Process()
        {
            switch (Args.Action)
            {
                case AccountAction.Info:
                    return await LoadAccountInfo();
                case AccountAction.New:
                    return await NewAccount();
                case AccountAction.Deactivate:
                    return await DeactivateAccount();
            }

            throw new NotSupportedException();
        }

        private async Task<object> DeactivateAccount()
        {
            var key = await Settings.GetAccountKey(Args, true);

            Logger.Debug("Using ACME server {0}.", Args.Server);
            var ctx = ContextFactory.Create(Args.Server, key);
            var acctCtx = await ctx.Account();

            Logger.Debug("Deactivate account at {0}", acctCtx.Location);
            var acct = await acctCtx.Deactivate();
            await Settings.SetAcmeSettings(new AcmeSettings
            {
                ServerUri = Args.Server,
                AccountKey = null
            }, Args);

            return new
            {
                uri = acctCtx.Location,
                data = acct,
            };
        }

        private async Task<object> NewAccount()
        {
            var key = await Settings.GetAccountKey(Args);
            if (key != null && !Args.Force)
            {
                throw new Exception("An account key already exists, use '--force' option to overwrite the existing key.");
            }

            Logger.Debug("Using ACME server {0}.", Args.Server);
            var ctx = ContextFactory.Create(Args.Server, null);

            Logger.Debug("Creating new account, email='{0}', agree='{1}'", Args.Email, Args.AgreeTos);
            var acctCtx = await ctx.NewAccount(Args.Email, Args.AgreeTos);
            Logger.Debug("Created new account at {0}", acctCtx.Location);

            await Settings.SetAcmeSettings(new AcmeSettings
            {
                AccountKey = ctx.AccountKey.ToPem(),
                ServerUri = Args.Server
            }, Args);

            return new
            {
                uri = acctCtx.Location,
                data = await acctCtx.Resource(),
            };
        }

        private async Task<object> LoadAccountInfo()
        {
            var key = await Settings.GetAccountKey(Args, true);

            Logger.Debug("Using ACME server {0}.", Args.Server);
            var ctx = ContextFactory.Create(Args.Server, key);
            var acctCtx = await ctx.Account();

            Logger.Debug("Retrieve account at {0}", acctCtx.Location);
            return new
            {
                uri = acctCtx.Location,
                data = await acctCtx.Resource(),
            };
        }
    }
}

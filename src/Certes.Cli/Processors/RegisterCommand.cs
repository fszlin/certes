using Certes.Acme;
using Certes.Cli.Options;
using Certes.Jws;
using NLog;
using System;
using System.Threading.Tasks;

namespace Certes.Cli.Processors
{
    internal class RegisterCommand : CommandBase<RegisterOptions>
    {
        public RegisterCommand(RegisterOptions options)
            : base(options)
        {
        }

        public override async Task<CliContext> Process(CliContext context)
        {
            if (Options.Thumbprint)
            {
                ShowThumbprint(context);
            }
            else if (Options.Update)
            {
                context = await UpdateAccount(context);
            }
            else
            {
                context = await RegisterAccount(context);
            }

            return context;
        }

        private async Task<CliContext> RegisterAccount(CliContext context)
        {
            if (context != null && !Options.Force)
            {
                throw new Exception($"A config exists at {Options.Path}, use a different path or --force option.");
            }

            var client = ContextFactory.CreateClient(Options.Server);

            var account = Options.NoEmail ?
                await client.NewRegistraton() :
                await client.NewRegistraton($"mailto:{Options.Email}");
            if (Options.AgreeTos)
            {
                account.Data.Agreement = account.GetTermsOfServiceUri();
                account = await client.UpdateRegistration(account);
            }

            context = new CliContext
            {
                Account = account
            };

            ConsoleLogger.Info("Registration created.");
            return context;
        }

        private async Task<CliContext> UpdateAccount(CliContext context)
        {
            bool changed = false;
            var account = context.Account;

            var client = ContextFactory.CreateClient(Options.Server);
            client.Use(account.Key);

            if (!string.IsNullOrWhiteSpace(Options.Email))
            {
                account.Data.Contact = new[] { $"mailto:{Options.Email}" };
                changed = true;
            }

            var currentTos = account.GetTermsOfServiceUri();
            if (Options.AgreeTos && account.Data.Agreement != currentTos)
            {
                account.Data.Agreement = currentTos;
                changed = true;
            }

            if (changed)
            {
                account = await client.UpdateRegistration(account);
            }

            ConsoleLogger.Info("Registration updated.");
            return context;
        }

        private void ShowThumbprint(CliContext context)
        {
            var account = new AccountKey(context.Account.Key);
            ConsoleLogger.Info(account.Thumbprint());
        }
    }
}

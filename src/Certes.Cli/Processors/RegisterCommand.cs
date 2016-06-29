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
        public RegisterCommand(RegisterOptions options, ILogger consoleLogger)
            : base(options, consoleLogger)
        {
        }
        
        public override async Task<AcmeContext> Process(AcmeContext context)
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

        private async Task<AcmeContext> RegisterAccount(AcmeContext context)
        {
            if (context != null && !Options.Force)
            {
                throw new Exception($"A config exists at {Options.Path}, use a different path or --force option.");
            }

            using (var client = new AcmeClient(Options.Server))
            {
                var account = await client.NewRegistraton($"mailto:{Options.Email}");
                if (Options.AgreeTos)
                {
                    account.Data.Agreement = account.GetTermsOfServiceUri();
                    account = await client.UpdateRegistration(account);
                }

                context = new AcmeContext
                {
                    Account = account
                };
            }

            return context;
        }

        private async Task<AcmeContext> UpdateAccount(AcmeContext context)
        {
            bool changed = false;
            var account = context.Account;
            using (var client = new AcmeClient(Options.Server))
            {
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
            }

            return context;
        }
        
        private void ShowThumbprint(AcmeContext context)
        {
            var account = new AccountKey(context.Account.Key);
            var thumbprint = JwsConvert.ToBase64String(account.GenerateThumbprint());
            ConsoleLogger.Info(thumbprint);
        }
    }
}

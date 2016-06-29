using Certes.Acme;
using Certes.Cli.Options;
using Certes.Jws;
using Certes.Pkcs;
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
                if (!string.IsNullOrWhiteSpace(Options.FromKey))
                {
                    var keyInfo = new KeyInfo
                    {
                        PrivateKeyInfo = Convert.FromBase64String(Options.FromKey)
                    };

                    client.Use(keyInfo);
                }

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

            ConsoleLogger.Info("Registration created.");
            return context;
        }

        private async Task<AcmeContext> UpdateAccount(AcmeContext context)
        {
            bool changed = false;
            var account = context?.Account;
            using (var client = new AcmeClient(Options.Server))
            {
                if (!string.IsNullOrWhiteSpace(Options.FromKey))
                {
                    if (context != null && !Options.Force)
                    {
                        throw new Exception($"A config exists at {Options.Path}, use a different path or --force option.");
                    }

                    var keyInfo = new KeyInfo
                    {
                        PrivateKeyInfo = Convert.FromBase64String(Options.FromKey)
                    };

                    client.Use(keyInfo);
                    changed = true;

                    context = new AcmeContext();
                    account = context.Account = new AcmeAccount();
                }
                else if (account != null)
                {
                    client.Use(account.Key);
                }
                else
                {
                    throw new Exception("Account not specified.");
                }

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

            ConsoleLogger.Info("Registration updated.");
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

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AccountNewCommand : CommandBase, ICliCommand
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountNewCommand));

        public AccountNewCommand(IUserSettings userSettings, AcmeContextFactory contextFactory, IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public CommandGroup Group => CommandGroup.Account;

        public Command Define()
        {
            var cmd = new Command("new", Strings.HelpCommandAccountNew)
            {
                new Option("--email", Strings.HelpEmail) { IsRequired = true },
                new Option(new [] { "--out-path", "--out" }, Strings.HelpKeyOut),
                new Option(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
            };

            cmd.Handler = CommandHandler.Create(async (string email, string outPath, Uri server, string keyPath, IConsole console) =>
            {
                var acct = await ReadAccountKey(server, keyPath);

                logger.Debug("Creating new account on '{0}'.", acct.Server);
                var key = acct.Key ?? KeyFactory.NewKey(KeyAlgorithm.ES256);

                var acme = ContextFactory.Invoke(acct.Server, key);
                var acctCtx = await acme.NewAccount(email, true);

                if (!string.IsNullOrWhiteSpace(outPath))
                {
                    logger.Debug("Saving new account key to '{0}'.", outPath);
                    var pem = key.ToPem();
                    await File.WriteAllText(outPath, pem);
                }
                else
                {
                    logger.Debug("Saving new account key to user settings.");
                    await UserSettings.SetAccountKey(acct.Server, key);
                }

                var output = new
                {
                    location = acctCtx.Location,
                    resource = await acctCtx.Resource()
                };

                console.WriteAsJson(output);
            });

            return cmd;
        }
    }
}

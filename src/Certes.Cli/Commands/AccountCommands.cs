using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AccountCommands: CommandsBase, ICliCommandFactory
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountCommands));
        private readonly AcmeContextFactory contextFactory;
        private readonly IUserSettings userSettings;

        public AccountCommands(IUserSettings userSettings, AcmeContextFactory contextFactory, IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
            this.userSettings = userSettings;
            this.contextFactory = contextFactory;
        }

        public Command Create()
        {
            var cmd = new Command(CommandGroup.Account.Command, CommandGroup.Account.Help);

            cmd.AddCommand(CreateNewCommand());
            cmd.AddCommand(CreateShowCommand());
            cmd.AddCommand(CreateSetCommand());
            cmd.AddCommand(CreateUpdateCommand());

            return cmd;
        }

        private Command CreateNewCommand()
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

        private Command CreateShowCommand()
        {
            var cmd = new Command("show", Strings.HelpCommandAccountShow)
            {
                new Option(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
            };

            cmd.Handler = CommandHandler.Create(async (Uri server, string keyPath, IConsole console) =>
            {
                var (serverUri, key) = await ReadAccountKey(server, keyPath, true, true);

                logger.Debug("Loading account from '{0}'.", serverUri);

                var acme = ContextFactory.Invoke(serverUri, key);
                var acctCtx = await acme.Account();

                var output = new
                {
                    location = acctCtx.Location,
                    resource = await acctCtx.Resource()
                };

                console.WriteAsJson(output);
            });

            return cmd;
        }

        private Command CreateSetCommand()
        {
            var cmd = new Command("set", Strings.HelpCommandAccountSet)
            {
                new Option(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
            };

            cmd.Handler = CommandHandler.Create(async (Uri server, string keyPath, IConsole console) =>
            {
                var (serverUri, key) = await ReadAccountKey(server, keyPath, false);

                logger.Debug("Setting account for '{0}'.", serverUri);

                var acme = ContextFactory.Invoke(serverUri, key);
                var acctCtx = await acme.Account();

                await UserSettings.SetAccountKey(serverUri, key);
                var output = new
                {
                    location = acctCtx.Location,
                    resource = await acctCtx.Resource()
                };

                console.WriteAsJson(output);
            });

            return cmd;
        }

        private Command CreateUpdateCommand()
        {
            var cmd = new Command("update", Strings.HelpCommandAccountUpdate)
            {
                new Option("--email", Strings.HelpEmail) { IsRequired = true },
                new Option(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
            };

            cmd.Handler = CommandHandler.Create(async (string email, Uri server, string keyPath, IConsole console) =>
            {
                var (serverUri, key) = await ReadAccountKey(server, keyPath, true, true);
                logger.Debug("Updating account on '{0}'.", serverUri);

                var acme = ContextFactory.Invoke(serverUri, key);
                var acctCtx = await acme.Account();
                var acct = await acctCtx.Update(new[] { $"mailto://{email}" }, true);

                var output = new
                {
                    location = acctCtx.Location,
                    resource = acct,
                };

                console.WriteAsJson(output);
            });

            return cmd;
        }

    }
}

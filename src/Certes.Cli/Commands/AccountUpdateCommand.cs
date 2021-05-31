using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AccountUpdateCommand : CommandBase, ICliCommand
    {
        public record Args(string Email, Uri Server, string KeyPath);

        private readonly ILogger logger = LogManager.GetLogger(nameof(AccountUpdateCommand));

        public AccountUpdateCommand(IUserSettings userSettings, AcmeContextFactory contextFactory, IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public CommandGroup Group => CommandGroup.Account;

        public Command Define()
        {
            var cmd = new Command("update", Strings.HelpCommandAccountUpdate)
            {
                new Argument<string>("email", Strings.HelpEmail),
                new Option<Uri>(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
            };

            cmd.Handler = CommandHandler.Create(async (Args args, IConsole console) =>
            {
                var (email, server, keyPath) = args;
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

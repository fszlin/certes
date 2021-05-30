using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AccountShowCommand : CommandBase, ICliCommand
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountShowCommand));

        public AccountShowCommand(IUserSettings userSettings, AcmeContextFactory contextFactory, IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public CommandGroup Group => CommandGroup.Account;

        public Command Define()
        {
            var cmd = new Command("show", Strings.HelpCommandAccountShow)
            {
                new Option<Uri>(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option<string>(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
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
    }
}

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class AccountSetCommand : CommandBase, ICliCommand
    {
        public record Args(Uri Server, string KeyPath);

        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountSetCommand));

        public AccountSetCommand(IUserSettings userSettings, AcmeContextFactory contextFactory, IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public CommandGroup Group => CommandGroup.Account;

        public Command Define()
        {
            var cmd = new Command("set", Strings.HelpCommandAccountSet)
            {
                new Option<Uri>(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Argument<string>("key-path", Strings.HelpKey),
            };

            cmd.Handler = CommandHandler.Create(async (Args args, IConsole console) =>
            {
                var (server, keyPath) = args;
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
    }
}

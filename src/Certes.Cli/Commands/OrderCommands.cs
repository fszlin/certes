using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class OrderCommands: CommandsBase, ICliCommandFactory
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountCommands));

        public OrderCommands(IUserSettings userSettings, AcmeContextFactory contextFactory, IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public Command Create()
        {
            var cmd = new Command(CommandGroup.Account.Command, CommandGroup.Account.Help);

            cmd.AddCommand(CreateAuthzCommand());
            cmd.AddCommand(CreateFinalizeCommand());
            cmd.AddCommand(CreateListCommand());
            cmd.AddCommand(CreateNewCommand());
            cmd.AddCommand(CreateShowCommand());
            cmd.AddCommand(CreateValidateCommand());

            return cmd;
        }

        private Command CreateAuthzCommand()
        {
            var cmd = new Command("authz", Strings.HelpCommandOrderAuthz)
            {
                new Option<Uri>("--order-id", Strings.HelpOrderId) { IsRequired = true },
                new Option("--domain", Strings.HelpDomain) { IsRequired = true },
                new Option("--challenge-type", Strings.HelpChallengeType) { IsRequired = true },
                new Option(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
            };

            cmd.Handler = CommandHandler.Create(async (Uri orderId, string domain, string challengeType, Uri server, string keyPath, IConsole console) =>
            {
                var (serverUri, key) = await ReadAccountKey(server, keyPath, true, false);

                var type =
                    string.Equals(challengeType, "dns", StringComparison.OrdinalIgnoreCase) ? ChallengeTypes.Dns01 :
                    string.Equals(challengeType, "http", StringComparison.OrdinalIgnoreCase) ? ChallengeTypes.Http01 :
                    throw new CertesCliException(string.Format(Strings.ErrorInvalidChallengeType, challengeType));

                logger.Debug("Loading authz from '{0}'.", serverUri);

                var acme = ContextFactory.Invoke(serverUri, key);
                var orderCtx = acme.Order(orderId);
                var authzCtx = await orderCtx.Authorization(domain)
                    ?? throw new CertesCliException(string.Format(Strings.ErrorIdentifierNotAvailable, domain));
                var challengeCtx = await authzCtx.Challenge(type)
                    ?? throw new CertesCliException(string.Format(Strings.ErrorChallengeNotAvailable, type));

                var challenge = await challengeCtx.Resource();

                if (string.Equals(type, ChallengeTypes.Dns01, StringComparison.OrdinalIgnoreCase))
                {
                    var output = new
                    {
                        location = challengeCtx.Location,
                        dnsTxt = key.DnsTxt(challenge.Token),
                        resource = challenge,
                    };

                    console.WriteAsJson(output);
                }
                else
                {
                    var output = new
                    {
                        location = challengeCtx.Location,
                        challengeFile = $".well-known/acme-challenge/{challenge.Token}",
                        challengeTxt = $"{challenge.Token}.{key.Thumbprint()}",
                        resource = challenge,
                        keyAuthz = challengeCtx.KeyAuthz
                    };

                    console.WriteAsJson(output);
                }
            });

            return cmd;
        }

        private Command CreateShowCommand()
        {
            throw new NotImplementedException();
        }

        private Command CreateNewCommand()
        {
            throw new NotImplementedException();
        }

        private Command CreateListCommand()
        {
            throw new NotImplementedException();
        }

        private Command CreateFinalizeCommand()
        {
            throw new NotImplementedException();
        }

        private Command CreateValidateCommand()
        {
            throw new NotImplementedException();
        }
    }
}

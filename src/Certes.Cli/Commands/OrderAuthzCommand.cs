using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class OrderAuthzCommand: CommandBase, ICliCommand
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(OrderAuthzCommand));

        public CommandGroup Group => CommandGroup.Order;

        public OrderAuthzCommand(IUserSettings userSettings, AcmeContextFactory contextFactory, IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public Command Define()
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
    }
}

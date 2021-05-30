using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

using static System.StringComparison;
using static Certes.Acme.Resource.ChallengeTypes;

namespace Certes.Cli.Commands
{
    internal class OrderValidateCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "validate";
        private const string OrderIdParam = "order-id";
        private const string DomainParam = "domain";
        private const string ChallengeTypeParam = "challenge-type";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(OrderValidateCommand));

        public CommandGroup Group { get; } = CommandGroup.Order;

        public OrderValidateCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public Command Define()
        {
            var cmd = new Command(CommandText, Strings.HelpCommandOrderValidate)
            {
                new Option<Uri>(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option<string>(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
                new Argument<Uri>(OrderIdParam, Strings.HelpOrderId),
                new Argument<string>(DomainParam, Strings.HelpDomain),
                new Argument<string>(ChallengeTypeParam, Strings.HelpChallengeType),
            };

            cmd.Handler = CommandHandler.Create(
                async (Uri orderId, string domain, string challengeType, Uri server, string keyPath, IConsole console) =>
            {
                var (serverUri, key) = await ReadAccountKey(server, keyPath, true, false);

                var type =
                    string.Equals(challengeType, "dns", OrdinalIgnoreCase) ? Dns01 :
                    string.Equals(challengeType, "http", OrdinalIgnoreCase) ? Http01 :
                    throw new CertesCliException(string.Format(Strings.ErrorInvalidChallengeType, challengeType));

                logger.Debug("Validating authz on '{0}'.", serverUri);

                var acme = ContextFactory.Invoke(serverUri, key);
                var orderCtx = acme.Order(orderId);
                var authzCtx = await orderCtx.Authorization(domain)
                    ?? throw new CertesCliException(string.Format(Strings.ErrorIdentifierNotAvailable, domain));
                var challengeCtx = await authzCtx.Challenge(type)
                    ?? throw new CertesCliException(string.Format(Strings.ErrorChallengeNotAvailable, challengeType));

                logger.Debug("Validating challenge '{0}'.", challengeCtx.Location);
                var challenge = await challengeCtx.Validate();

                var output = new
                {
                    location = challengeCtx.Location,
                    resource = challenge,
                };

                console.WriteAsJson(output);
            });

            return cmd;
        }
    }
}

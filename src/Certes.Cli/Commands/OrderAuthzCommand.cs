using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

using static System.StringComparison;
using static Certes.Acme.Resource.ChallengeTypes;

namespace Certes.Cli.Commands
{
    internal class OrderAuthzCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "authz";
        private const string OrderIdParam = "order-id";
        private const string DomainParam = "domain";
        private const string ChallengeTypeParam = "challenge-type";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(OrderAuthzCommand));

        public CommandGroup Group { get; } = CommandGroup.Order;

        public OrderAuthzCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandOrderAuthz);

            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId)
                .DefineParameter(DomainParam, help: Strings.HelpDomain)
                .DefineParameter(ChallengeTypeParam, help: Strings.HelpChallengeType);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, false);
            var orderUri = syntax.GetParameter<Uri>(OrderIdParam, true);
            var domain = syntax.GetParameter<string>(DomainParam, true);
            var typeStr = syntax.GetParameter<string>(ChallengeTypeParam, true);

            var type =
                string.Equals(typeStr, "dns", OrdinalIgnoreCase) ? Dns01 :
                string.Equals(typeStr, "http", OrdinalIgnoreCase) ? Http01 :
                throw new ArgumentSyntaxException(string.Format(Strings.ErrorInvalidChallengeType, typeStr));

            logger.Debug("Loading authz from '{0}'.", serverUri);

            var acme = ContextFactory.Invoke(serverUri, key);
            var orderCtx = acme.Order(orderUri);
            var authzCtx = await orderCtx.Authorization(domain)
                ?? throw new CertesCliException(string.Format(Strings.ErrorIdentifierNotAvailable, domain));
            var challengeCtx = await authzCtx.Challenge(type)
                ?? throw new CertesCliException(string.Format(Strings.ErrorChallengeNotAvailable, typeStr));

            var challenge = await challengeCtx.Resource();

            if (string.Equals(type, Dns01, OrdinalIgnoreCase))
            {
                return new
                {
                    location = challengeCtx.Location,
                    dnsTxt = key.DnsTxt(challenge.Token),
                    resource = challenge,
                };
            }

            return new
            {
                location = challengeCtx.Location,
                resource = challenge,
                keyAuthz = challengeCtx.KeyAuthz
            };
        }
    }
}

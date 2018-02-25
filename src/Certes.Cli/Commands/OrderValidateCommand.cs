using System;
using System.CommandLine;
using System.Threading.Tasks;
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
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandOrderValidate);

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

            logger.Debug("Validating authz on '{0}'.", serverUri);

            var acme = ContextFactory.Create(serverUri, key);
            var orderCtx = acme.Order(orderUri);
            var authzCtx = await orderCtx.Authorization(domain)
                ?? throw new Exception(string.Format(Strings.ErrorIdentifierNotAvailable, domain));
            var challengeCtx = await authzCtx.Challenge(type)
                ?? throw new Exception(string.Format(Strings.ErrorChallengeNotAvailable, type));
            var challenge = await challengeCtx.Validate();

            return new
            {
                location = challengeCtx.Location,
                resource = challenge,
            };
        }
    }
}

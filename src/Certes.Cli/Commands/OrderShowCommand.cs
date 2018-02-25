using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class OrderShowCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "show";
        private const string OrderIdParam = "order-id";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(OrderShowCommand));

        public CommandGroup Group { get; } = CommandGroup.Order;

        public OrderShowCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandOrderShow);

            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, false);
            var orderUri = syntax.GetParameter<Uri>(OrderIdParam, true);

            logger.Debug("Loading order from '{0}'.", serverUri);

            var acme = ContextFactory.Create(serverUri, key);
            var orderCtx = acme.Order(orderUri);

            return new
            {
                location = orderCtx.Location,
                resource = await orderCtx.Resource()
            };
        }
    }
}

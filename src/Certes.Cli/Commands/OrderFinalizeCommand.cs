using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class OrderFinalizeCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "finalize";
        private const string OrderIdParam = "order-id";
        private const string OutOption = "out";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(OrderFinalizeCommand));

        public CommandGroup Group { get; } = CommandGroup.Order;

        public OrderFinalizeCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandOrderFinalize);

            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineOption(OutOption, help: Strings.HelpOut)
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, false);
            var orderUri = syntax.GetParameter<Uri>(OrderIdParam, true);
            var outPath = syntax.GetOption<string>(OutOption);

            var certKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            await File.WriteAllText(outPath, certKey.ToPem());

            logger.Debug("Loading order from '{0}'.", serverUri);

            var acme = ContextFactory.Create(serverUri, key);
            var orderCtx = acme.Order(orderUri);
            var order = await orderCtx.Finalize(new CsrInfo(), certKey);

            return new
            {
                location = orderCtx.Location,
                resource = order,
            };
        }
    }
}

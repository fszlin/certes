using System.Collections.Generic;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class OrderListCommand : CommandBase//, ICliCommand
    {
        private const string CommandText = "list";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(OrderListCommand));

        public CommandGroup Group { get; } = CommandGroup.Order;

        public OrderListCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandOrderList);

            syntax
                .DefineServerOption()
                .DefineKeyOption();

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, true);

            logger.Debug("Loading orders from '{0}'.", serverUri);

            var acme = ContextFactory.Invoke(serverUri, key);
            var acctCtx = await acme.Account();
            var orderListCtx = await acctCtx.Orders();

            var orderList = new List<object>();
            foreach (var orderCtx in await orderListCtx.Orders())
            {
                orderList.Add(new
                {
                    location = orderCtx.Location,
                    resource = await orderCtx.Resource()
                });
            }

            return orderList;
        }
    }
}

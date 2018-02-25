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
        private const string DnOption = "dn";
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
                .DefineOption(DnOption, help: Strings.HelpDn)
                .DefineOption(OutOption, help: Strings.HelpKeyOut)
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, false);
            var orderUri = syntax.GetParameter<Uri>(OrderIdParam, true);
            var distinguishedName = syntax.GetOption<string>(DnOption);
            var outPath = syntax.GetOption<string>(OutOption);

            var certKey = KeyFactory.NewKey(KeyAlgorithm.ES256);

            logger.Debug("Finalizing order from '{0}'.", serverUri);

            var acme = ContextFactory.Create(serverUri, key);
            var orderCtx = acme.Order(orderUri);

            var csr = await orderCtx.CreateCsr(certKey);
            if (!string.IsNullOrWhiteSpace(distinguishedName))
            {
                csr.AddName(distinguishedName);
            }

            var order = await orderCtx.Finalize(csr.Generate());

            if (string.IsNullOrWhiteSpace(outPath))
            {
                return new
                {
                    location = orderCtx.Location,
                    privateKey = certKey.ToDer(),
                    resource = order,
                };
            }
            else
            {
                await File.WriteAllText(outPath, certKey.ToPem());
                return new
                {
                    location = orderCtx.Location,
                    resource = order,
                };
            }
        }
    }
}

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
        private const string PrivateKeyOption = "private-key";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(OrderFinalizeCommand));
        private readonly IEnvironmentVariables environment;

        public CommandGroup Group { get; } = CommandGroup.Order;

        public OrderFinalizeCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil,
            IEnvironmentVariables environment)
            : base(userSettings, contextFactory, fileUtil)
        {
            this.environment = environment;
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandOrderFinalize);

            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineOption(DnOption, help: Strings.HelpDn)
                .DefineOption(OutOption, help: Strings.HelpKeyOut)
                .DefineOption(PrivateKeyOption, help: Strings.HelpPrivateKey)
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, false);
            var orderUri = syntax.GetParameter<Uri>(OrderIdParam, true);
            var distinguishedName = syntax.GetOption<string>(DnOption);
            var outPath = syntax.GetOption<string>(OutOption);

            var providedKey = await syntax.ReadKey(PrivateKeyOption, "CERTES_CERT_KEY", File, environment);
            var privKey = providedKey ?? KeyFactory.NewKey(KeyAlgorithm.ES256);

            logger.Debug("Finalizing order from '{0}'.", serverUri);

            var acme = ContextFactory.Create(serverUri, key);
            var orderCtx = acme.Order(orderUri);

            var csr = await orderCtx.CreateCsr(privKey);
            if (!string.IsNullOrWhiteSpace(distinguishedName))
            {
                csr.AddName(distinguishedName);
            }

            var order = await orderCtx.Finalize(csr.Generate());

            // output private key only if it is generated and not being saved
            if (string.IsNullOrWhiteSpace(outPath) && providedKey == null)
            {
                return new
                {
                    location = orderCtx.Location,
                    privateKey = privKey.ToDer(),
                    resource = order,
                };
            }
            else
            {
                if (providedKey == null)
                {
                    await File.WriteAllText(outPath, privKey.ToPem());
                }

                return new
                {
                    location = orderCtx.Location,
                    resource = order,
                };
            }
        }
    }
}

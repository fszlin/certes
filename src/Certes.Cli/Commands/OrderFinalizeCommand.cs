using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class OrderFinalizeCommand : CommandBase, ICliCommand
    {
        public record Args
        {
            public Uri OrderId { get; init; }
            public KeyAlgorithm KeyAlgorithm { get; init; }
            public string PrivateKey { get; init; }
            public string OutPath { get; init; }
            public string Dn { get; init; }
            public Uri Server { get; init; }
            public string KeyPath { get; init; }
        }

        private const string CommandText = "finalize";
        private const string OrderIdParam = "--order-id";
        private const string DnOption = "--dn";
        private const string PrivateKeyOption = "--private-key";
        private const string KeyAlgorithmOption = "--key-algorithm";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(OrderFinalizeCommand));
        private readonly IEnvironmentVariables environment;

        public CommandGroup Group { get; } = CommandGroup.Order;

        public OrderFinalizeCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil,
            IEnvironmentVariables environment)
            : base(userSettings, contextFactory, fileUtil)
        {
            this.environment = environment;
        }

        public Command Define()
        {
            var cmd = new Command(CommandText, Strings.HelpCommandOrderFinalize)
            {
                new Argument<Uri>(OrderIdParam, Strings.HelpOrderId),
                new Option<Uri>(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option<string>(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
                new Option<string>(DnOption, Strings.HelpDn),
                new Option<string>(new [] { "--out-path", "--out" }, Strings.HelpKeyOut),
                new Option<string>(PrivateKeyOption, Strings.HelpPrivateKey),
                new Option<KeyAlgorithm>(KeyAlgorithmOption, () => KeyAlgorithm.ES256, Strings.HelpKeyAlgorithm),
            };

            cmd.Handler = CommandHandler.Create(
                (Args args, IConsole console) =>
                Execute(args, console));

            return cmd;
        }

        private async Task Execute(Args args, IConsole console)
        {
            var (serverUri, key) = await ReadAccountKey(args.Server, args.KeyPath, true, false);
            var providedKey = await ReadKey(args.PrivateKey, "CERTES_CERT_KEY", File, environment);
            var privKey = providedKey ?? KeyFactory.NewKey(args.KeyAlgorithm);

            logger.Debug("Finalizing order from '{0}'.", serverUri);

            var acme = ContextFactory.Invoke(serverUri, key);
            var orderCtx = acme.Order(args.OrderId);

            var csr = await orderCtx.CreateCsr(privKey);
            if (!string.IsNullOrWhiteSpace(args.Dn))
            {
                csr.AddName(args.Dn);
            }

            var order = await orderCtx.Finalize(csr.Generate());

            // output private key only if it is generated and not being saved
            if (string.IsNullOrWhiteSpace(args.OutPath) && providedKey == null)
            {
                var output = new
                {
                    location = orderCtx.Location,
                    privateKey = privKey.ToDer(),
                    resource = order,
                };

                console.WriteAsJson(output);
            }
            else
            {
                if (providedKey == null)
                {
                    await File.WriteAllText(args.OutPath, privKey.ToPem());
                }

                var output = new
                {
                    location = orderCtx.Location,
                    resource = order,
                };

                console.WriteAsJson(output);
            }
        }
    }
}

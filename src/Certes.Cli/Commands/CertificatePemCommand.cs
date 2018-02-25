using System;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class CertificatePemCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "pem";
        private const string OrderIdParam = "order-id";
        private const string OutOption = "out";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(CertificatePemCommand));

        public CommandGroup Group { get; } = CommandGroup.Certificate;

        public CertificatePemCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandCertPem);

            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineOption(OutOption, help: Strings.HelpCertificateOut)
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, true);
            var orderUri = syntax.GetParameter<Uri>(OrderIdParam, true);

            logger.Debug("Downloading certificate from '{0}'.", serverUri);

            var acme = ContextFactory.Create(serverUri, key);
            var orderCtx = acme.Order(orderUri);
            var order = await orderCtx.Resource();
            if (order.Status != OrderStatus.Valid)
            {
                throw new Exception(
                    string.Format(Strings.ErrorExportInvalidOrder, order.Status));
            }

            var cert = await orderCtx.Download();

            var outPath = syntax.GetOption<string>(OutOption);
            if (string.IsNullOrWhiteSpace(outPath))
            {
                return new
                {
                    location = order.Certificate,
                    resource = new
                    {
                        certificate = cert.Certificate.ToDer(),
                        issuers = cert.Issuers.Select(i => i.ToDer()).ToArray(),
                    },
                };
            }
            else
            {
                logger.Debug("Saving certificate to '{0}'.", outPath);

                var buffer = new StringBuilder();
                buffer.AppendLine(cert.Certificate.ToPem());
                foreach (var issuer in cert.Issuers)
                {
                    buffer.AppendLine(issuer.ToPem());
                }

                await File.WriteAllText(outPath, buffer.ToString());

                return new
                {
                    location = order.Certificate,
                };

            }
        }
    }
}

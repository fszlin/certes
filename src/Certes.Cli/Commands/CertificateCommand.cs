using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal abstract class CertificateCommand : CommandBase
    {
        protected const string OrderIdParam = "order-id";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(CertificateCommand));

        public CommandGroup Group { get; } = CommandGroup.Certificate;

        public CertificateCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        protected async Task<(Uri Location, CertificateChain Cert)> DownloadCertificate(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, true);
            var orderUri = syntax.GetParameter<Uri>(OrderIdParam, true);

            logger.Debug("Downloading certificate from '{0}'.", serverUri);

            var acme = ContextFactory.Invoke(serverUri, key);
            var orderCtx = acme.Order(orderUri);
            var order = await orderCtx.Resource();
            if (order.Status != OrderStatus.Valid)
            {
                throw new Exception(
                    string.Format(Strings.ErrorExportInvalidOrder, order.Status));
            }

            return (order.Certificate, await orderCtx.Download());
        }
    }
}

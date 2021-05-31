using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal abstract class CertificateCommandBase : CommandBase, ICliCommand
    {
        protected const string OrderIdOption = "--order-id";
        protected const string PreferredChainOption = "--preferred-chain";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(CertificateCommandBase));

        public CertificateCommandBase(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public CommandGroup Group => CommandGroup.Certificate;

        public abstract Command Define();

        protected async Task<(Uri Location, CertificateChain Cert)> DownloadCertificate(Uri orderUri, string preferredChain, Uri server, string keyPath)
        {
            var (serverUri, key) = await ReadAccountKey(server, keyPath, true, true);

            logger.Debug("Downloading certificate from '{0}'.", serverUri);

            var acme = ContextFactory.Invoke(serverUri, key);
            var orderCtx = acme.Order(orderUri);
            var order = await orderCtx.Resource();
            if (order.Status != OrderStatus.Valid)
            {
                throw new CertesCliException(
                    string.Format(Strings.ErrorExportInvalidOrder, order.Status));
            }

            return (order.Certificate, await orderCtx.Download(preferredChain));
        }
    }
}

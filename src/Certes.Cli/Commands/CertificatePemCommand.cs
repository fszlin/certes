using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class CertificatePemCommand : CertificateCommand, ICliCommand
    {
        private const string CommandText = "pem";
        private const string OutOption = "out";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(CertificatePemCommand));

        public CertificatePemCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandCertificatePem);

            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineOption(OutOption, help: Strings.HelpCertificateOut)
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (location, cert) = await DownloadCertificate(syntax);

            var outPath = syntax.GetOption<string>(OutOption);
            if (string.IsNullOrWhiteSpace(outPath))
            {
                return new
                {
                    location,
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
                    location,
                };

            }
        }
    }
}

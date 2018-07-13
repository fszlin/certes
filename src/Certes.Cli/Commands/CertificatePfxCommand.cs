using System;
using System.CommandLine;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class CertificatePfxCommand : CertificateCommand, ICliCommand
    {
        private const string CommandText = "pfx";
        private const string PasswordParam = "password";
        private const string PrivateKeyOption = "private-key";
        private const string OutOption = "out";
        private const string IssuerOption = "issuer";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(CertificatePfxCommand));

        private readonly IEnvironmentVariables environment;

        public CertificatePfxCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil,
            IEnvironmentVariables environment)
            : base(userSettings, contextFactory, fileUtil)
        {
            this.environment = environment;
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandCertificatePfx);

            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineOption(OutOption, help: Strings.HelpCertificateOut)
                .DefineOption(PrivateKeyOption, help: Strings.HelpPrivateKey)
                .DefineOption(IssuerOption, help: Strings.HelpCertificateIssuer)
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId)
                .DefineParameter(PasswordParam, help: Strings.HelpPfxPassword);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var keyPath = syntax.GetParameter<string>(PrivateKeyOption, true);
            var pwd = syntax.GetParameter<string>(PasswordParam, true);
            var issuer = syntax.GetParameter<string>(IssuerOption);
            var (location, cert) = await DownloadCertificate(syntax);

            var pfxName = string.Format(CultureInfo.InvariantCulture, "[certes] {0:yyyyMMddhhmmss}", DateTime.UtcNow);
            var privKey = await syntax.ReadKey(PrivateKeyOption, "CERTES_CERT_KEY", File, environment, true);

            var pfxBuilder = cert.ToPfx(privKey);
            if (!string.IsNullOrWhiteSpace(issuer))
            {
                var issuerPem = await File.ReadAllText(issuer);
                pfxBuilder.AddIssuers(Encoding.UTF8.GetBytes(issuerPem));
            }

            var pfx = pfxBuilder.Build(pfxName, pwd);
            
            var outPath = syntax.GetOption<string>(OutOption);
            if (string.IsNullOrWhiteSpace(outPath))
            {
                return new
                {
                    location,
                    pfx,
                };
            }
            else
            {
                logger.Debug("Saving certificate to '{0}'.", outPath);
                await File.WriteAllBytes(outPath, pfx);

                return new
                {
                    location,
                };

            }
        }
    }
}

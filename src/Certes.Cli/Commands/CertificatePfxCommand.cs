using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class CertificatePfxCommand : CertificateCommand, ICliCommand
    {
        private const string CommandText = "pfx";
        private const string PasswordParam = "password";
        private const string PrivateKeyParam = "private-key";
        private const string OutOption = "out";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(CertificatePfxCommand));

        public CertificatePfxCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandCertificatePfx);

            syntax
                .DefineServerOption()
                .DefineKeyOption()
                .DefineOption(OutOption, help: Strings.HelpCertificateOut)
                .DefineUriParameter(OrderIdParam, help: Strings.HelpOrderId)
                .DefineParameter(PrivateKeyParam, help: Strings.HelpPrivateKey)
                .DefineParameter(PasswordParam, help: Strings.HelpPfxPassword);

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var keyPath = syntax.GetParameter<string>(PrivateKeyParam, true);
            var pwd = syntax.GetParameter<string>(PasswordParam, true);
            var (location, cert) = await DownloadCertificate(syntax);

            var privKey = KeyFactory.FromPem(await File.ReadAllText(keyPath));

            var pfx = cert.ToPfx(privKey).Build($"{location} by certes", pwd);
            
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

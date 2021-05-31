using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using System.Text;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class CertificatePfxCommand : CertificateCommandBase, ICliCommand
    {
        public record Args(
            Uri OrderId,
            string PrivateKey,
            string FriendlyName,
            string Issuer,
            string Password,
            string PreferredChain,
            string OutPath,
            Uri Server,
            string KeyPath);

        private const string CommandText = "pfx";
        private const string FriendlyNameOption = "--friendly-name";
        private const string PasswordParam = "--password";
        private const string PrivateKeyOption = "--private-key";
        private const string IssuerOption = "--issuer";

        private readonly ILogger logger = LogManager.GetLogger(nameof(CertificatePfxCommand));
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

        public override Command Define()
        {
            var cmd = new Command(CommandText, Strings.HelpCommandCertificatePem)
            {
                new Option<Uri>(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option<string>(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
                new Option<string>(new [] { "--out-path", "--out" }, Strings.HelpCertificateOut),
                new Option<string>(PrivateKeyOption, Strings.HelpPrivateKey),
                new Option<string>(FriendlyNameOption, Strings.HelpFriendlyName),
                new Option<string>(IssuerOption, Strings.HelpCertificateIssuer),
                new Option<string>(PreferredChainOption, Strings.HelpPreferredChain),
                new Argument<Uri>(OrderIdOption, Strings.HelpOrderId),
                new Argument<string>(PasswordParam, Strings.HelpPfxPassword),
            };

            cmd.Handler = CommandHandler.Create(async (
                Args args,
                IConsole console) =>
            {
                var (orderId, privateKey, friendlyName, issuer, password, preferredChain, outPath, server, keyPath) = args;
                var (location, cert) = await DownloadCertificate(orderId, preferredChain, server, keyPath);

                var privKey = await ReadKey(privateKey, "CERTES_CERT_KEY", File, environment);
                if (privKey == null)
                {
                    throw new CertesCliException(Strings.ErrorNoPrivateKey);
                }

                var pfxName = string.Format(CultureInfo.InvariantCulture, "[certes] {0:yyyyMMddhhmmss}", DateTime.UtcNow);
                if (!string.IsNullOrWhiteSpace(friendlyName))
                {
                    pfxName = string.Concat(friendlyName, " ", pfxName);
                }

                var pfxBuilder = cert.ToPfx(privKey);
                if (!string.IsNullOrWhiteSpace(issuer))
                {
                    var issuerPem = await File.ReadAllText(issuer);
                    pfxBuilder.AddIssuers(Encoding.UTF8.GetBytes(issuerPem));
                }

                var pfx = pfxBuilder.Build(pfxName, password);

                if (string.IsNullOrWhiteSpace(outPath))
                {
                    var output = new
                    {
                        location,
                        pfx,
                    };

                    console.WriteAsJson(output);
                }
                else
                {
                    logger.Debug("Saving certificate to '{0}'.", outPath);
                    await File.WriteAllBytes(outPath, pfx);

                    var output = new
                    {
                        location,
                    };

                    console.WriteAsJson(output);
                }
            });

            return cmd;
        }
    }
}

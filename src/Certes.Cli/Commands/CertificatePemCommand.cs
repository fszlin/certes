using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class CertificatePemCommand : CertificateCommandBase, ICliCommand
    {
        private readonly ILogger logger = LogManager.GetLogger(nameof(CertificatePemCommand));

        public CertificatePemCommand(IUserSettings userSettings, AcmeContextFactory contextFactory, IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public override Command Define()
        {
            var cmd = new Command("pem", Strings.HelpCommandCertificatePem)
            {
                new Argument<Uri>(OrderIdOption, Strings.HelpOrderId),
                new Option<string>(PreferredChainOption, Strings.HelpPreferredChain),
                new Option<string>(new [] { "--out-path", "--out" }, Strings.HelpKeyOut),
                new Option<string>(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option<string>(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
            };

            cmd.Handler = CommandHandler.Create(async (
                Uri orderId,
                string preferredChain,
                string outPath,
                Uri server,
                string keyPath,
                IConsole console) =>
            {
                var (location, cert) = await DownloadCertificate(orderId, preferredChain, server, keyPath);

                if (string.IsNullOrWhiteSpace(outPath))
                {
                    var output = new
                    {
                        location,
                        resource = new
                        {
                            certificate = cert.Certificate.ToDer(),
                            issuers = cert.Issuers.Select(i => i.ToDer()).ToArray(),
                        },
                    };

                    console.WriteAsJson(output);
                }
                else
                {
                    logger.Debug("Saving certificate to '{0}'.", outPath);
                    await File.WriteAllText(outPath, cert.ToPem());

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

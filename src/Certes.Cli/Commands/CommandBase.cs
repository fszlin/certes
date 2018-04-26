using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal abstract class CommandBase
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(CommandBase));
        protected IFileUtil File { get; private set; }
        protected IUserSettings UserSettings { get; private set; }
        protected AcmeContextFactory ContextFactory { get; private set; }

        protected CommandBase(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
        {
            UserSettings = userSettings;
            ContextFactory = contextFactory;
            File = fileUtil;
        }

        protected async Task<(Uri Server, IKey Key)> ReadAccountKey(
            ArgumentSyntax syntax, bool fallbackToSettings = false, bool required = false)
        {
            var serverUri = syntax.GetServerOption() ??
                await UserSettings.GetDefaultServer();

            var keyPath = syntax.GetKeyOption();
            if (!string.IsNullOrWhiteSpace(keyPath))
            {
                logger.Debug("Load account key form '{0}'.", keyPath);
                var pem = await File.ReadAllText(keyPath);
                return (serverUri, KeyFactory.FromPem(pem));
            }

            var key = fallbackToSettings ?
                await UserSettings.GetAccountKey(serverUri) : null;
            
            if (required && key == null)
            {
                throw new CertesCliException(
                    string.Format(Strings.ErrorNoAccountKey, serverUri));
            }

            return (serverUri, key);
        }
    }
}

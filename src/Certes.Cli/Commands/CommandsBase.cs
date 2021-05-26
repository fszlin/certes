using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal abstract class CommandsBase
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(CommandsBase));
        protected IFileUtil File { get; private set; }
        protected IUserSettings UserSettings { get; private set; }
        protected AcmeContextFactory ContextFactory { get; private set; }

        protected CommandsBase(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
        {
            UserSettings = userSettings;
            ContextFactory = contextFactory;
            File = fileUtil;
        }

        protected async Task<(Uri Server, IKey Key)> ReadAccountKey(
            Uri server, string keyPath = null, bool fallbackToSettings = false, bool required = false)
        {
            var serverUri = server ??
                await UserSettings.GetDefaultServer();

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

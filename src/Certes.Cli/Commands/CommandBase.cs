using System;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal abstract class CommandBase
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(CommandBase));
        public IFileUtil File { get; init; }
        public IUserSettings UserSettings { get; init; }
        public AcmeContextFactory ContextFactory { get; init; }

        protected CommandBase(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
        {
            UserSettings = userSettings;
            ContextFactory = contextFactory;
            File = fileUtil;
        }

        public async Task<(Uri Server, IKey Key)> ReadAccountKey(
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

        public static async Task<IKey> ReadKey(
            string keyPath,
            string environmentVariableName,
            IFileUtil file,
            IEnvironmentVariables environment)
        {
            if (!string.IsNullOrWhiteSpace(keyPath))
            {
                return KeyFactory.FromPem(await file.ReadAllText(keyPath));
            }
            else
            {
                var keyData = environment.GetVar(environmentVariableName);
                if (!string.IsNullOrWhiteSpace(keyData))
                {
                    return KeyFactory.FromDer(Convert.FromBase64String(keyData));
                }
            }

            return null;
        }
    }
}

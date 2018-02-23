using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class CommandBase
    {
        private static readonly ILogger logger = LogManager.GetLogger(nameof(CommandBase));
        protected IFileUtil File { get; private set; }
        protected IUserSettings UserSettings { get; private set; }
        protected IAcmeContextFactory ContextFactory { get; private set; }

        protected CommandBase(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
        {
            UserSettings = userSettings;
            ContextFactory = contextFactory;
            File = fileUtil;
        }

        protected async Task<(Uri Server, IKey Key)> ReadAccountKey(
            ArgumentSyntax syntax)
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

            return (serverUri, null);
        }
    }
}

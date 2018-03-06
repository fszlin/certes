using System;
using System.CommandLine;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal abstract class CommandBase
    {
        private static Regex Base64Regex = new Regex("^[a-zA-Z0-9+/]+={0,2}$", RegexOptions.Compiled);

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
            ArgumentSyntax syntax, bool fallbackToSettings = false, bool required = false)
        {
            var serverUri = syntax.GetServerOption() ??
                await UserSettings.GetDefaultServer();

            var keyValue = syntax.GetKeyOption();
            if (!string.IsNullOrWhiteSpace(keyValue))
            {
                var likeBase64 = Base64Regex.IsMatch(keyValue);
                if (likeBase64)
                {
                    var der = Convert.FromBase64String(keyValue);
                    return (serverUri, KeyFactory.FromDer(der));
                }
                else
                {
                    logger.Debug("Load account key form '{0}'.", keyValue);
                    var pem = await File.ReadAllText(keyValue);
                    return (serverUri, KeyFactory.FromPem(pem));
                }
            }

            var key = fallbackToSettings ?
                await UserSettings.GetAccountKey(serverUri) : null;
            
            if (required && key == null)
            {
                throw new Exception(
                    string.Format(Strings.ErrorNoAccountKey, serverUri));
            }

            return (serverUri, key);
        }
    }
}

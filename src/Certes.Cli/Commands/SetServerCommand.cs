using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    // e.x: set-server https://acme-staging-v02.api.letsencrypt.org/directory
    internal class SetServerCommand : ICliCommand
    {
        private const string ParamUri = "server-uri";
        private readonly ILogger logger = LogManager.GetLogger(nameof(SetServerCommand));

        private Uri serverUri;

        public async Task<object> Execute()
        {
            var ctx = ContextFactory.Create(serverUri, null);
            logger.Debug("Loading directory from '{0}'", serverUri);
            var directory = await ctx.GetDirectory();
            await UserSettings.SetServer(serverUri);
            return new
            {
                location = serverUri,
                directory,
            };
        }

        public bool Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand("set-server");
            cmd.Help = Strings.SetServerHelp;

            var uriParam = syntax.DefineParameter<Uri>(
                ParamUri, ref serverUri, Strings.ServerUriHelper);

            if (string.IsNullOrWhiteSpace(cmd.Value))
            {
                return false;
            }

            if (serverUri == null)
            {
                syntax.ReportError(string.Format(Strings.ParameterMissing, ParamUri));
            }

            return true;
        }
    }
}

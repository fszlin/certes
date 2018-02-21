using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    // e.x: set-server https://acme-staging-v02.api.letsencrypt.org/directory
    internal class SetServerCommand : ICliCommand
    {
        private const string CommandText = "set-server";
        private const string ParamUri = "server-uri";
        private readonly ILogger logger = LogManager.GetLogger(nameof(SetServerCommand));

        private readonly Func<Uri, IKey, IAcmeContext> contextFactory;

        public IUserSettings Settings { get; private set; }

        public Uri ServerUri { get; private set; }

        public SetServerCommand(IUserSettings userSettings, Func<Uri, IKey, IAcmeContext> contextFactory = null)
        {
            Settings = userSettings;
            this.contextFactory = contextFactory ?? ContextFactory.Create;
        }

        public async Task<object> Execute()
        {
            var ctx = contextFactory(ServerUri, null);
            logger.Debug("Loading directory from '{0}'", ServerUri);
            var directory = await ctx.GetDirectory();
            await Settings.SetServer(ServerUri);
            return new
            {
                location = ServerUri,
                directory,
            };
        }

        public bool Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText);
            cmd.Help = Strings.SetServerHelp;

            var arg = syntax.DefineParameter(
                ParamUri, WellKnownServers.LetsEncryptStagingV2, s => new Uri(s));
            arg.Help = Strings.ServerUriHelper;

            if (!cmd.IsActive)
            {
                return false;
            }

            if (!arg.IsSpecified)
            {
                syntax.ReportError(string.Format(Strings.ParameterMissing, ParamUri));
            }

            ServerUri = arg.Value;
            return true;
        }
    }
}

using System;
using System.CommandLine;
using System.Linq;
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
        
        public SetServerCommand(IUserSettings userSettings, Func<Uri, IKey, IAcmeContext> contextFactory = null)
        {
            Settings = userSettings;
            this.contextFactory = contextFactory ?? ContextFactory.Create;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var serverUriParam = syntax.GetActiveParameters()
                .Where(p => p.Name == ParamUri)
                .OfType<Argument<Uri>>()
                .First();

            if (!serverUriParam.IsSpecified)
            {
                syntax.ReportError(string.Format(Strings.ParameterMissing, ParamUri));
            }

            var ctx = contextFactory(serverUriParam.Value, null);
            logger.Debug("Loading directory from '{0}'", serverUriParam.Value);
            var directory = await ctx.GetDirectory();
            await Settings.SetServer(serverUriParam.Value);
            return new
            {
                location = serverUriParam.Value,
                directory,
            };
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText);
            cmd.Help = Strings.SetServerHelp;

            var arg = syntax.DefineParameter(
                ParamUri, WellKnownServers.LetsEncryptV2, Strings.ServerUriHelper);
            
            return cmd;
        }
    }
}

using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class OrderNewCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "new";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(AccountNewCommand));

        public CommandGroup Group { get; } = CommandGroup.Order;

        public OrderNewCommand(
            IUserSettings userSettings,
            IAcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public ArgumentCommand<string> Define(ArgumentSyntax syntax)
        {
            var cmd = syntax.DefineCommand(CommandText, help: Strings.HelpCommandOrderNew);

            syntax
                .DefineServerOption()
                .DefineKeyOption();

            var domainsParam = syntax.DefineParameterList("domains", new string[0]);
            domainsParam.Help = Strings.HelpDomains;

            return cmd;
        }

        public async Task<object> Execute(ArgumentSyntax syntax)
        {
            var (serverUri, key) = await ReadAccountKey(syntax, true, true);

            var domains = syntax.GetActiveArguments()
                .Where(a => a.Name == "domains")
                .OfType<ArgumentList<string>>()
                .Select(a => a.Value)
                .FirstOrDefault();

            logger.Debug("Creating order from '{0}'.", serverUri);

            var acme = ContextFactory.Create(serverUri, key);
            var orderCtx = await acme.NewOrder(domains.ToArray());

            return new
            {
                location = orderCtx.Location,
                resource = await orderCtx.Resource(),
            };
        }
    }
}

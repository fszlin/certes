using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class OrderShowCommand : CommandBase, ICliCommand
    {
        private const string CommandText = "show";
        private const string OrderIdParam = "order-id";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(OrderShowCommand));

        public CommandGroup Group { get; } = CommandGroup.Order;

        public OrderShowCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public Command Define()
        {
            var cmd = new Command(CommandText, Strings.HelpCommandOrderShow)
            {
                new Option<Uri>(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option<string>(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
                new Argument<Uri>(OrderIdParam, Strings.HelpOrderId),
            };

            cmd.Handler = CommandHandler.Create(async (
                Uri orderId,
                Uri server,
                string keyPath,
                IConsole console) =>
            {
                var (serverUri, key) = await ReadAccountKey(server, keyPath, true, false);
                logger.Debug("Loading order from '{0}'.", serverUri);

                var acme = ContextFactory.Invoke(serverUri, key);
                var orderCtx = acme.Order(orderId);

                var output = new
                {
                    location = orderCtx.Location,
                    resource = await orderCtx.Resource()
                };

                console.WriteAsJson(output);
            });

            return cmd;
        }
    }
}

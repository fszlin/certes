﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class OrderNewCommand : CommandBase, ICliCommand
    {
        public record Args(IList<string> domains, Uri Server, string KeyPath);

        private const string CommandText = "new";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(OrderNewCommand));

        public CommandGroup Group { get; } = CommandGroup.Order;

        public OrderNewCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public Command Define()
        {
            var cmd = new Command(CommandText, Strings.HelpCommandOrderNew)
            {
                new Option<Uri>(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option<string>(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
                new Argument<IList<string>>("domains", Strings.HelpDomains),
            };

            cmd.Handler = CommandHandler.Create(async (Args args, IConsole console) =>
            {
                var (domains, server, keyPath) = args;
                var (serverUri, key) = await ReadAccountKey(server, keyPath, true, false);
                logger.Debug("Creating order from '{0}'.", serverUri);

                var acme = ContextFactory.Invoke(serverUri, key);
                var orderCtx = await acme.NewOrder(domains);

                var output = new
                {
                    location = orderCtx.Location,
                    resource = await orderCtx.Resource(),
                };

                console.WriteAsJson(output);
            });

            return cmd;
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Commands
{
    internal class OrderListCommand : CommandBase, ICliCommand
    {
        public record Args(Uri Server, string KeyPath);

        private const string CommandText = "list";
        private static readonly ILogger logger = LogManager.GetLogger(nameof(OrderListCommand));

        public CommandGroup Group { get; } = CommandGroup.Order;

        public OrderListCommand(
            IUserSettings userSettings,
            AcmeContextFactory contextFactory,
            IFileUtil fileUtil)
            : base(userSettings, contextFactory, fileUtil)
        {
        }

        public Command Define()
        {
            var cmd = new Command(CommandText, Strings.HelpCommandOrderList)
            {
                new Option<Uri>(new[]{ "--server", "-s" }, Strings.HelpServer),
                new Option<string>(new[]{ "--key-path", "--key", "-k" }, Strings.HelpKey),
            };

            cmd.Handler = CommandHandler.Create(async (Args args, IConsole console) =>
            {
                var (server, keyPath) = args;
                var (serverUri, key) = await ReadAccountKey(server, keyPath, true, false);
                logger.Debug("Loading orders from '{0}'.", serverUri);

                var acme = ContextFactory.Invoke(serverUri, key);
                var acctCtx = await acme.Account();
                var orderListCtx = await acctCtx.Orders();

                var orderList = new List<object>();
                foreach (var orderCtx in await orderListCtx.Orders())
                {
                    orderList.Add(new
                    {
                        location = orderCtx.Location,
                        resource = await orderCtx.Resource()
                    });
                }

                console.WriteAsJson(orderList);
            });

            return cmd;
        }
    }
}

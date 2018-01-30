using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Certes.Cli.Options;
using Certes.Cli.Settings;
using NLog;

namespace Certes.Cli.Processors
{
    internal class OrderCommand
    {
        public ILogger Logger { get; } = LogManager.GetCurrentClassLogger();
        private OrderOptions Args { get; }

        public OrderCommand(OrderOptions args)
        {
            Args = args;
        }

        public static OrderOptions TryParse(ArgumentSyntax syntax)
        {
            var options = new OrderOptions();

            var command = Command.Undefined;
            syntax.DefineCommand("order", ref command, Command.Order, "Manange ACME orders.");
            if (command == Command.Undefined)
            {
                return null;
            }

            syntax.DefineEnumOption("validate", ref options.Validate, $"Validate the authz.");
            syntax.DefineOption<Uri>("uri", ref options.Location, $"The order resource's URI.");
            
            syntax.DefineOption<Uri>("server", ref options.Server, $"ACME Directory Resource URI.");
            syntax.DefineOption("key", ref options.Path, $"File path to the account key to use.");
            syntax.DefineOption("force", ref options.Force, $"Overwrite exising account key.");

            syntax.DefineEnumParameter("action", ref options.Action, "Order action");
            syntax.DefineParameterList("name", ref options.Values, "Domain names");

            return options;
        }

        public async Task<object> Process()
        {
            switch (Args.Action)
            {
                case OrderAction.Info:
                    return await ShowOrder();
                case OrderAction.New:
                    return await NewOrder();
                case OrderAction.Authz:
                    return await ProcessAuthz();
            }

            throw new NotSupportedException();
        }

        private async Task<object> ShowOrder()
        {
            var key = await UserSettings.GetAccountKey(Args, true);

            Logger.Debug("Using ACME server {0}.", Args.Server);
            var ctx = ContextFactory.Create(Args.Server, key);

            var orderCtx = ctx.Order(Args.Location);
            return new
            {
                uri = orderCtx.Location,
                data = await orderCtx.Resource()
            };
        }

        private async Task<object> ProcessAuthz()
        {
            var key = await UserSettings.GetAccountKey(Args, true);
            var name = Args.Values[0];

            Logger.Debug("Using ACME server {0}.", Args.Server);
            var ctx = ContextFactory.Create(Args.Server, key);

            var orderCtx = ctx.Order(Args.Location);
            var authzCtx = await orderCtx.Authorization(name);
            if (authzCtx == null)
            {
                throw new Exception($"Authz not found for {name}.");
            }

            var challengeCtx =
                Args.Validate == AuthorizationType.Dns ? await authzCtx.Dns() :
                Args.Validate == AuthorizationType.Http ? await authzCtx.Http() :
                null;

            if (challengeCtx != null)
            {
                await challengeCtx.Validate();
            }
            else if (Args.Validate != AuthorizationType.Unspecific)
            {
                throw new Exception($"{Args.Validate} challenge for {name} not found.");
            }

            return new
            {
                uri = authzCtx.Location,
                data = await authzCtx.Resource()
            };
        }

        private async Task<object> NewOrder()
        {
            var key = await UserSettings.GetAccountKey(Args, true);

            Logger.Debug("Using ACME server {0}.", Args.Server);
            var ctx = ContextFactory.Create(Args.Server, key);

            var orderCtx = await ctx.NewOrder(Args.Values.ToArray());

            Logger.Debug("Created new order at {0}", orderCtx.Location);
            return new
            {
                uri = orderCtx.Location,
                data = await orderCtx.Resource(),
            };
        }
    }
}

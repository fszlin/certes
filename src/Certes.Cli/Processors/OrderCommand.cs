using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Cli.Options;
using Certes.Cli.Settings;
using Certes.Pkcs;
using NLog;

namespace Certes.Cli.Processors
{
    internal class OrderCommand
    {
        public ILogger Logger { get; } = LogManager.GetCurrentClassLogger();
        private OrderOptions Args { get; }
        public UserSettings Settings { get; private set; }

        public OrderCommand(OrderOptions args, UserSettings userSettings)
        {
            Args = args;
            Settings = userSettings;
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
            syntax.DefineOption("cert-key", ref options.CertKeyPath, $"File path to the certificate private key to use.");
            syntax.DefineOption("dn", ref options.DistinguishName, $"Distinguish name for the certificate.");
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
                case OrderAction.List:
                    return await ListOrder();
                case OrderAction.New:
                    return await NewOrder();
                case OrderAction.Authz:
                    return await ProcessAuthz();
                case OrderAction.Finalize:
                    return await FinalizeOrder();
            }

            throw new NotSupportedException();
        }

        private async Task<object> FinalizeOrder()
        {
            var key = await Settings.GetAccountKey(Args, true);
            Logger.Debug("Using ACME server {0}.", Args.Server);
            var ctx = ContextFactory.Create(Args.Server, key);

            var orderCtx = ctx.Order(Args.Location);

            var certKeyPem = await FileUtil.ReadAllText(Args.CertKeyPath);
            var certKey = certKeyPem == null ?
                KeyFactory.NewKey(KeyAlgorithm.RS256) :
                KeyFactory.FromPem(certKeyPem);

            var csrBuilder = new CertificationRequestBuilder(certKey);
            csrBuilder.AddName(Args.DistinguishName);

            var order = await orderCtx.Finalize(csrBuilder.Generate());

            // key output path is specificed
            if (certKeyPem == null && !string.IsNullOrWhiteSpace(Args.CertKeyPath))
            {
                await FileUtil.WriteAllTexts(Args.CertKeyPath, certKey.ToPem());
            }

            return new
            {
                uri = orderCtx.Location,
                data = order,
                certKey = string.IsNullOrWhiteSpace(Args.CertKeyPath) ? certKey.ToPem() : null
            };
        }

        private async Task<object> ListOrder()
        {
            var key = await Settings.GetAccountKey(Args, true);

            Logger.Debug("Using ACME server {0}.", Args.Server);
            var ctx = ContextFactory.Create(Args.Server, key);

            var accountCtx = await ctx.Account();
            var orderListCtx = await accountCtx.Orders();

            var orders = new List<object>();
            foreach (var orderCtx in await orderListCtx.Orders())
            {
                orders.Add(new
                {
                    uri = orderCtx.Location,
                    data = await orderCtx.Resource(),
                });
            }

            return orders;
        }

        private async Task<object> ShowOrder()
        {
            var key = await Settings.GetAccountKey(Args, true);

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
            var key = await Settings.GetAccountKey(Args, true);
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

            if (challengeCtx != null) // if validate option is set
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
            var key = await Settings.GetAccountKey(Args, true);

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;

namespace Certes.Acme
{
    internal class OrderContext : EntityContext<Order>, IOrderContext
    {
        public OrderContext(
            IAcmeContext context,
            Uri location)
            : base(context, location)
        {
        }

        /// <summary>
        /// Authorizationses this instance.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<IAuthorizationContext>> Authorizations()
        {
            var order = await Resource();
            return order
                .Authorizations?
                .Select(a => new AuthorizationContext(Context, a)) ??
                Enumerable.Empty<IAuthorizationContext>();
        }

        public async Task<Order> Finalize(byte[] csr)
        {
            var order = await Resource();
            var payload = await Context.Sign(new Order { Csr = JwsConvert.ToBase64String(csr) }, order.Finalize);
            var resp = await Context.HttpClient.Post<Order>(order.Finalize, payload, true);
            return resp.Resource;
        }

        public async Task<string> Download()
        {
            var order = await Resource();
            var resp = await Context.HttpClient.Get<string>(order.Certificate);
            return resp.Resource;
        }
    }
}

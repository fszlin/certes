using Certes.Acme.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Acme
{
    internal class OrderContext : IOrderContext
    {
        private readonly IAcmeContext context;
        private readonly Uri location;

        public OrderContext(
            IAcmeContext context,
            Uri location)
        {
            this.context = context;
            this.location = location;
        }

        /// <summary>
        /// Resources this instance.
        /// </summary>
        /// <returns></returns>
        public async Task<Order> Resource()
        {
            var resp = await this.context.HttpClient.Get<Order>(location);
            return resp.Resource;
        }

        /// <summary>
        /// Authorizationses this instance.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<AuthorizationContext>> Authorizations()
        {
            var order = await this.Resource();
            return order.Authorizations
                .Select(a => new AuthorizationContext(this.context, a));
        }
    }
}

using Certes.Acme.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Certes.Acme
{
    internal class OrderContext : IOrderContext
    {
        private readonly AcmeContext context;
        private readonly IAccountContext account;
        private readonly Uri location;

        public OrderContext(
            AcmeContext context,
            IAccountContext account,
            Uri location)
        {
            this.context = context;
            this.account = account;
            this.location = location;
        }

        /// <summary>
        /// Resources this instance.
        /// </summary>
        /// <returns></returns>
        public async Task<Order> Resource()
        {
            var (_, order, _) = await this.context.Get<Order>(location);
            return order;
        }

        /// <summary>
        /// Authorizationses this instance.
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<AuthorizationContext>> Authorizations()
        {
            var order = await this.Resource();
            return order.Authorizations
                .Select(a => new AuthorizationContext(this.context, this.account, a));
        }
    }
}

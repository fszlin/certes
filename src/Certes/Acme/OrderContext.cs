using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;

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
    }
}

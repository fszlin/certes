using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;

namespace Certes.Acme
{
    internal class OrderListContext : EntityContext<OrderList>, IOrderListContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrderListContext"/> class.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="location">The location.</param>
        public OrderListContext(IAcmeContext context, Uri location)
            : base(context, location)
        {
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public async Task<IEnumerable<IOrderContext>> Orders()
        {
            var orderList = new List<IOrderContext>();
            var next = Location;
            while (next != null)
            {
                var resp = await Context.HttpClient.Get<OrderList>(next);

                orderList.AddRange(
                    resp.Resource.Orders.Select(o => new OrderContext(Context, o)));

                next = resp.Links["next"].FirstOrDefault();
            }

            return orderList;
        }
    }
}

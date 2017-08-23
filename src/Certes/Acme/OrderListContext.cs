using Certes.Acme.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Certes.Acme.IOrderListContext" />
    public class OrderListContext : IOrderListContext
    {
        private readonly AcmeContext context;
        private readonly IAccountContext account;
        private readonly Uri location;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="account"></param>
        /// <param name="location"></param>
        public OrderListContext(
            AcmeContext context,
            IAccountContext account,
            Uri location)
        {
            this.context = context;
            this.account = account;
            this.location = location;
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
            var next = this.location;
            while (next != null)
            {
                var (_, orders, links) = await this.context.Get<OrderList>(next);

                orderList.AddRange(
                    orders.Orders.Select(o => new OrderContext(this.context, this.account, o)));

                next = links["next"].FirstOrDefault();
            }

            return orderList;
        }

        /// <summary>
        /// Creates a new order the specified CSR.
        /// </summary>
        /// <param name="csr">The CSR.</param>
        /// <param name="notBefore">The not before timestamp.</param>
        /// <param name="notAfter">The not after timestamp.</param>
        /// <returns>
        /// The created order.
        /// </returns>
        public async Task<IOrderContext> New(string csr, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            var endpoint = await this.context.GetResourceEndpoint(ResourceType.NewOrder);
            if (endpoint == null)
            {
                throw new NotSupportedException();
            }

            var body = new Dict
            {
                { "csr", csr },
                { "notBefore", notBefore },
                { "notAfter", notAfter },
            };

            var payload = this.account.Sign(body, endpoint);
            var (location, _, _) = await this.context.Post<Order>(endpoint, payload);

            return new OrderContext(this.context, this.account, location);
        }
    }
}

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
        private readonly IAcmeContext context;
        private readonly IAccountContext account;
        private readonly Uri location;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="account"></param>
        /// <param name="location"></param>
        public OrderListContext(
            IAcmeContext context,
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
                var resp = await this.context.HttpClient.Get<OrderList>(next);

                orderList.AddRange(
                    resp.Resource.Orders.Select(o => new OrderContext(this.context, this.account, o)));

                next = resp.Links["next"].FirstOrDefault();
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
            var dir = await context.GetDirectory();
            var endpoint = dir.NewOrder;
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

            var payload = await context.Sign(body, endpoint);
            var resp = await this.context.HttpClient.Post<Order>(endpoint, payload);

            return new OrderContext(this.context, this.account, resp.Location);
        }
    }
}

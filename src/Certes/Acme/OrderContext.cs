using System;

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
    }
}
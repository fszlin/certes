using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the context for ACME order operations.
    /// </summary>
    /// <seealso cref="Certes.Acme.IOrderContext" />
    internal class OrderContext : EntityContext<Order>, IOrderContext
    {
        public OrderContext(
            IAcmeContext context,
            Uri location)
            : base(context, location)
        {
        }

        /// <summary>
        /// Gets the authorizations for this order.
        /// </summary>
        /// <returns>
        /// The list of authorizations.
        /// </returns>
        public async Task<IEnumerable<IAuthorizationContext>> Authorizations()
        {
            var order = await Resource();
            return order
                .Authorizations?
                .Select(a => new AuthorizationContext(Context, a)) ??
                Enumerable.Empty<IAuthorizationContext>();
        }

        /// <summary>
        /// Finalizes the certificate order.
        /// </summary>
        /// <param name="csr">The CSR in DER.</param>
        /// <returns>
        /// The order finalized.
        /// </returns>
        public async Task<Order> Finalize(byte[] csr)
        {
            var order = await Resource();
            // var payload = await Context.Sign(new Order.Payload { Csr = JwsConvert.ToBase64String(csr) }, order.Finalize);
            // var resp = await Context.HttpClient.Post<Order>(order.Finalize, payload, true);
            var payload = new Order.Payload { Csr = JwsConvert.ToBase64String(csr) };
            var resp = await Context.HttpClient.Post<Order>(Context, order.Finalize, payload, true);
            return resp.Resource;
        }

        /// <summary>
        /// Downloads the certificate chain in PEM.
        /// </summary>
        /// <returns>The certificate chain in PEM.</returns>
        public async Task<CertificateChain> Download()
        {
            var order = await Resource();
            // var payload = await Context.Sign(null, order.Certificate);
            // var resp = await Context.HttpClient.Post<string>(order.Certificate, payload, false);
            var resp = await Context.HttpClient.Post<string>(Context, order.Certificate, null, false);

            return new CertificateChain(resp.Resource);
        }
    }
}

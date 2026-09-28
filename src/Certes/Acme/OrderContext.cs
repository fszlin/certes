using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
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
        public async Task<IEnumerable<IAuthorizationContext>> Authorizations(CancellationToken cancellationToken = default)
        {
            var order = await Resource(cancellationToken);
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
        /// <param name="cancellationToken">Cancels this operation.</param>
        public async Task<Order> Finalize(byte[] csr, CancellationToken cancellationToken = default)
        {
            var order = await Resource(cancellationToken);
            var payload = new Order.Payload { Csr = JwsConvert.ToBase64String(csr) };
            var resp = await Context.HttpClient.Post<Order>(Context, order.Finalize, payload, true, cancellationToken);
            RetryAfter = resp.RetryAfter;
            return resp.Resource;
        }

        /// <summary>
        /// Downloads the certificate chain in PEM.
        /// <param name="preferredChain">The preferred Root Certificate</param>
        /// </summary>
        /// <returns>The certificate chain in PEM.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        public async Task<CertificateChain> Download(string preferredChain = null, CancellationToken cancellationToken = default)
        {
            var order = await Resource(cancellationToken);
            var resp = await Context.HttpClient.Post<string>(Context, order.Certificate, null, false, cancellationToken);

            var defaultChain = new CertificateChain(resp.Resource);
            if (defaultChain.MatchesPreferredChain(preferredChain) || resp.Links == null || !resp.Links.Contains("alternate"))
                return defaultChain;

            var alternateLinks = resp.Links["alternate"].ToList();
            foreach (var alternate in alternateLinks)
            {
                resp = await Context.HttpClient.Post<string>(Context, alternate, null, false, cancellationToken);
                var chain = new CertificateChain(resp.Resource);

                if (chain.MatchesPreferredChain(preferredChain))
                    return chain;
            }

            return defaultChain;
        }

    }
}

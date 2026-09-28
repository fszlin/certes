using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Certes.Acme.Resource;

namespace Certes.Acme
{
    /// <summary>
    /// Presents the context for ACME order operations.
    /// </summary>
    public interface IOrderContext : IResourceContext<Order>
    {
        /// <summary>
        /// Gets the authorizations for this order.
        /// </summary>
        /// <returns>
        /// The list of authorizations.
        /// </returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<IEnumerable<IAuthorizationContext>> Authorizations(CancellationToken cancellationToken = default);

        /// <summary>
        /// Finalizes the certificate order.
        /// </summary>
        /// <param name="csr">The CSR in DER.</param>
        /// <returns>The order finalized.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<Order> Finalize(byte[] csr, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Downloads the certificate chain in PEM.
        /// </summary>
        /// <param name="preferredChain">The preferred Root Certificate.</param>
        /// <returns>The certificate chain in PEM.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<CertificateChain> Download(string preferredChain = null, CancellationToken cancellationToken = default);
    }
}

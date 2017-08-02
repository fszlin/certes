using System;
using System.Threading.Tasks;
using Certes.Jws;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the HTTP handler for communicating with ACME server.
    /// </summary>
    public interface IAcmeHttpHandler
    {
        /// <summary>
        /// Gets the ACME server URI.
        /// </summary>
        /// <value>
        /// The ACME server URI.
        /// </value>
        Uri ServerUri { get; }

        /// <summary>
        /// Gets the ACME resource URI.
        /// </summary>
        /// <param name="resourceType">Type of the ACME resource.</param>
        /// <returns>The ACME resource URI</returns>
        Task<Uri> GetResourceUri(string resourceType);

        /// <summary>
        /// Performs HTTP GET to <paramref name="uri"/>.
        /// </summary>
        /// <typeparam name="T">The resource entity type.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <returns>The ACME response.</returns>
        Task<AcmeRespone<T>> Get<T>(Uri uri);

        /// <summary>
        /// Performs HTTP POST to <paramref name="uri"/>.
        /// </summary>
        /// <typeparam name="T">The resource entity type.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="keyPair">The signing key pair.</param>
        /// <returns>The ACME response.</returns>
        Task<AcmeRespone<T>> Post<T>(Uri uri, T entity, IAccountKey keyPair);
    }
}
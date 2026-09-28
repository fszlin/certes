using System;
using System.Threading.Tasks;
using System.Threading;

namespace Certes.Acme
{
    /// <summary>
    /// Supports loading ACME resource with URI.
    /// </summary>
    /// <typeparam name="T">The resource entity type.</typeparam>
    public interface IResourceContext<T>
    {
        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <value>
        /// The location.
        /// </value>
        Uri Location { get; }

        /// <summary>
        /// The timespan after which to retry the request
        /// </summary>
        int RetryAfter { get; }

        /// <summary>
        /// Gets the ACME resource.
        /// </summary>
        /// <returns>The resource entity.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<T> Resource(CancellationToken cancellationToken = default);
    }
}

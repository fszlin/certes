using System;
using System.Threading.Tasks;

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
        Task<T> Resource();
    }
}

using System.Net;

namespace Certes.Acme
{

    /// <summary>
    /// Represents a ACME entity response from the server.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <seealso cref="Certes.Acme.AcmeResult{T}" />
    public class AcmeRespone<T> : AcmeResult<T>
    {
        /// <summary>
        /// Gets or sets the replay nonce.
        /// </summary>
        /// <value>
        /// The replay nonce.
        /// </value>
        public string ReplayNonce { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status.
        /// </summary>
        /// <value>
        /// The HTTP status.
        /// </value>
        public HttpStatusCode HttpStatus { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public AcmeError Error { get; set; }
    }
}

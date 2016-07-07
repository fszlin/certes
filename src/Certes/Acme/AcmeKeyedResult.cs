using Certes.Pkcs;

namespace Certes.Acme
{
    /// <summary>
    /// Represents a ACME entity returned from the server with the key pair.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <seealso cref="Certes.Acme.AcmeResult{T}" />
    public class KeyedAcmeResult<T> : AcmeResult<T>
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public KeyInfo Key { get; set; }
    }
}

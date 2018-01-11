using System;

namespace Certes.Pkcs
{
    /// <summary>
    /// The supported algorithms.
    /// </summary>
    [Obsolete("Use KeyAlgorithm instead.")]
    public enum SignatureAlgorithm
    {
        /// <summary>
        /// SHA256 hash with RSA encryption.
        /// </summary>
        [Obsolete("Use KeyAlgorithm.RS256 instead.")]
        Sha256WithRsaEncryption = KeyAlgorithm.RS256,
    }
}

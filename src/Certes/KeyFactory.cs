using Certes.Crypto;

namespace Certes
{
    /// <summary>
    /// Provides helper methods for handling keys.
    /// </summary>
    public static class KeyFactory
    {
        private static readonly KeyAlgorithmProvider keyAlgorithmProvider = new KeyAlgorithmProvider();

        /// <summary>
        /// Creates a random key.
        /// </summary>
        /// <param name="algorithm">The algorithm to use.</param>
        /// <param name="keySize">Optional key size (used for RSA, defaults to 2048)</param>
        /// <returns>The key created.</returns>
        public static IKey NewKey(KeyAlgorithm algorithm, int? keySize = null)
        {
            var algo = keyAlgorithmProvider.Get(algorithm);
            return algo.GenerateKey(keySize);
        }

        /// <summary>
        /// Parse the key from DER encoded data.
        /// </summary>
        /// <param name="der">The DER encoded data.</param>
        /// <returns>The key restored.</returns>
        public static IKey FromDer(byte[] der) =>
            keyAlgorithmProvider.GetKey(der);

        /// <summary>
        /// Parse the key from PEM encoded text.
        /// </summary>
        /// <param name="pem">The PEM encoded text.</param>
        /// <returns>The key restored.</returns>
        public static IKey FromPem(string pem) =>
            keyAlgorithmProvider.GetKey(pem);

        /// <summary>
        /// Gets the signer for the given key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The signer.</returns>
        internal static ISigner GetSigner(this IKey key)
        {
            var algorithm = keyAlgorithmProvider.Get(key.Algorithm);
            return algorithm.CreateSigner(key);
        }
    }
}

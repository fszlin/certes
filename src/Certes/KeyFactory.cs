using Certes.Crypto;
using Certes.Properties;
using Org.BouncyCastle.X509;

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

        /// <summary>
        /// Verifies that the private key belongs to the certificate. This checks
        /// key ownership only; it does not validate the certificate or its chain.
        /// </summary>
        /// <exception cref="AcmeException">Thrown when the key does not match the certificate.</exception>
        internal static void EnsureKeyMatches(this IKey key, X509Certificate certificate)
        {
            var (_, keyPair) = keyAlgorithmProvider.GetKeyPair(key.ToDer());
            if (!keyPair.Public.Equals(certificate.GetPublicKey()))
            {
                throw new AcmeException(Strings.ErrorPrivateKeyMismatch);
            }
        }

        /// <summary>
        /// Gets the key pair from the key, after verifying that it matches the certificate.
        /// </summary>
        /// <param name="key">The key to load.</param>
        /// <param name="certificate">The certificate to verify against.</param>
        /// <returns>The loaded key pair.</returns>
        /// <exception cref="AcmeException">Thrown when the key does not match the certificate.</exception>
        internal static Org.BouncyCastle.Crypto.AsymmetricCipherKeyPair GetKeyPairFor(this IKey key, X509Certificate certificate)
        {
            key.EnsureKeyMatches(certificate);
            var (_, keyPair) = keyAlgorithmProvider.GetKeyPair(key.ToDer());
            return keyPair;
        }
    }
}

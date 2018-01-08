using Certes.Crypto;
using Certes.Pkcs;

namespace Certes
{
    /// <summary>
    /// Provides helper methods for handling DSA keys.
    /// </summary>
    public static class DSA
    {
        private static readonly SignatureAlgorithmProvider signatureAlgorithmProvider = new SignatureAlgorithmProvider();

        /// <summary>
        /// News the key.
        /// </summary>
        /// <param name="algorithm">The algorithm.</param>
        /// <returns></returns>
        public static ISignatureKey NewKey(SignatureAlgorithm algorithm = SignatureAlgorithm.ES256)
        {
            var signatureAlgorithm = signatureAlgorithmProvider.Get(algorithm);
            return signatureAlgorithm.GenerateKey();
        }

        /// <summary>
        /// Reads the key.
        /// </summary>
        /// <param name="der">The DER.</param>
        /// <returns></returns>
        public static ISignatureKey FromDer(byte[] der) =>
            signatureAlgorithmProvider.GetKey(der);

        /// <summary>
        /// Froms the pem.
        /// </summary>
        /// <param name="pem">The pem.</param>
        /// <returns></returns>
        public static ISignatureKey FromPem(string pem) =>
            signatureAlgorithmProvider.GetKey(pem);

        internal static ISigner GetSigner(this ISignatureKey key)
        {
            var signatureAlgorithm = signatureAlgorithmProvider.Get(key.Algorithm);
            return signatureAlgorithm.CreateSigner(key);
        }
    }
}

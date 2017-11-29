using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;

namespace Certes.Jws
{
    internal abstract class JsonWebAlgorithm : IJsonWebAlgorithm
    {
        protected readonly AsymmetricCipherKeyPair keyPair;
        protected abstract string SigningAlgorithm { get; }
        protected abstract string HashAlgorithm { get; }
        public abstract JsonWebKey JsonWebKey { get; }

        public JsonWebAlgorithm(AsymmetricCipherKeyPair keyPair)
        {
            this.keyPair = keyPair;
        }

        /// <summary>
        /// Computes the hash for given data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The hash.</returns>
        public virtual byte[] ComputeHash(byte[] data) => DigestUtilities.CalculateDigest(HashAlgorithm, data);

        /// <summary>
        /// Signs the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The signature.</returns>
        public virtual byte[] SignData(byte[] data)
        {
            var signer = SignerUtilities.GetSigner(SigningAlgorithm);
            signer.Init(true, keyPair.Private);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.GenerateSignature();
        }
    }
}

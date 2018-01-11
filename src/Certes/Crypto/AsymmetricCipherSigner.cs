using System;
using Org.BouncyCastle.Security;

namespace Certes.Crypto
{
    internal abstract class AsymmetricCipherSigner : ISigner
    {
        protected AsymmetricCipherKey Key { get; private set; }

        public AsymmetricCipherSigner(IKey key)
        {
            Key = (key as AsymmetricCipherKey) ?? throw new ArgumentException(nameof(key));
        }
        protected abstract string SigningAlgorithm { get; }
        protected abstract string HashAlgorithm { get; }

        public virtual byte[] ComputeHash(byte[] data) => DigestUtilities.CalculateDigest(HashAlgorithm, data);

        public virtual byte[] SignData(byte[] data)
        {
            var signer = SignerUtilities.GetSigner(SigningAlgorithm);
            signer.Init(true, Key.KeyPair.Private);
            signer.BlockUpdate(data, 0, data.Length);
            return signer.GenerateSignature();
        }
    }

}

using System;
using Org.BouncyCastle.Crypto.Parameters;

namespace Certes.Crypto
{
    internal sealed class RS256Signer : AsymmetricCipherSigner
    {
        protected override string SigningAlgorithm => "SHA-256withRSA";

        protected override string HashAlgorithm => "SHA256";

        public RS256Signer(IKey key)
            : base(key)
        {
            if (!(Key.KeyPair.Private is RsaPrivateCrtKeyParameters))
            {
                throw new ArgumentException("The given key is not an RAS private key.", nameof(key));
            }
        }
    }

}

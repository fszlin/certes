using System;
using System.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Parameters;

namespace Certes.Crypto
{
    internal sealed class EllipticCurveSigner : AsymmetricCipherSigner
    {
        private readonly int fieldSize;

        public EllipticCurveSigner(IKey key, string signingAlgorithm, string hashAlgorithm)
            : base(key)
        {
            var privKey = Key.KeyPair.Private as ECPrivateKeyParameters;
            if (privKey == null)
            {
                throw new ArgumentException("The given key is not an EC private key.", nameof(key));
            }

            fieldSize = privKey.Parameters.Curve.FieldSize / 8;
            SigningAlgorithm = signingAlgorithm;
            HashAlgorithm = hashAlgorithm;
        }

        protected override string SigningAlgorithm { get; }

        protected override string HashAlgorithm { get; }

        public override byte[] SignData(byte[] data)
        {
            var signature = base.SignData(data);
            var sequence = (Asn1Sequence)Asn1Object.FromByteArray(signature);

            var nums = sequence
                .OfType<DerInteger>()
                .Select(i => i.Value.ToByteArrayUnsigned())
                .ToArray();

            var signatureBytes = new byte[fieldSize * nums.Length];

            for (var i = 0; i < nums.Length; ++i)
            {
                Array.Copy(
                    nums[i], 0, 
                    signatureBytes, fieldSize * (i + 1) - nums[i].Length, 
                    nums[i].Length);
            }

            return signatureBytes;
        }
    }
}

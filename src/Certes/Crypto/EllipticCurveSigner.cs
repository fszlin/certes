using System;
using System.IO;
using System.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Crypto.Parameters;

namespace Certes.Crypto
{
    internal sealed class EllipticCurveSigner : AsymmetricCipherSigner
    {
        public EllipticCurveSigner(ISignatureKey key, string signingAlgorithm, string hashAlgorithm)
            : base(key)
        {
            if (!(Key.KeyPair.Private is ECPrivateKeyParameters))
            {
                throw new ArgumentException("The given key is not an EC private key.", nameof(key));
            }

            SigningAlgorithm = signingAlgorithm;
            HashAlgorithm = hashAlgorithm;
        }

        protected override string SigningAlgorithm { get; }

        protected override string HashAlgorithm { get; }

        public override byte[] SignData(byte[] data)
        {
            var signature = base.SignData(data);
            var sequence = (Asn1Sequence)Asn1Object.FromByteArray(signature);
            using (var buffer = new MemoryStream())
            {
                foreach (var num in sequence
                    .OfType<DerInteger>()
                    .Select(i => i.Value))
                {
                    var bytes = num.ToByteArrayUnsigned();
                    for (var i = bytes.Length; i < 32; ++i)
                    {
                        buffer.WriteByte(0);
                    }

                    buffer.Write(bytes, 0, bytes.Length);
                }

                return buffer.ToArray();
            }
        }
    }
}

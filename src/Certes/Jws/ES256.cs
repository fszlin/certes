using System.IO;
using System.Linq;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Certes.Jws
{
    internal sealed class ES256 : JsonWebAlgorithm
    {
        public override JsonWebKey JsonWebKey
        {
            get
            {
                var parameters = (ECPublicKeyParameters)keyPair.Public;
                return new EcJsonWebKey
                {
                    KeyType = "EC",
                    Curve = "P-256",
                    X = JwsConvert.ToBase64String(parameters.Q.AffineXCoord.ToBigInteger().ToByteArrayUnsigned()),
                    Y = JwsConvert.ToBase64String(parameters.Q.AffineYCoord.ToBigInteger().ToByteArrayUnsigned()),
                };
            }
        }

        protected override string SigningAlgorithm => "SHA-256withECDSA";

        protected override string HashAlgorithm => "SHA256";

        public ES256(AsymmetricCipherKeyPair keyPair) 
            : base(keyPair)
        {
        }

        public override byte[] SignData(byte[] data)
        {
            var signature = base.SignData(data);
            var sequence = (Asn1Sequence)Asn1Object.FromByteArray(signature);
            using (var buffer = new MemoryStream())
            {
                foreach (var intBytes in sequence
                    .OfType<Asn1Object>()
                    .Select(o => o.ToAsn1Object())
                    .Cast<DerInteger>()
                    .Select(i => i.Value.ToByteArrayUnsigned()))
                {
                    buffer.Write(intBytes, 0, intBytes.Length);
                }

                return buffer.ToArray();
            }
        }

        public static AsymmetricCipherKeyPair CreateKeyPair()
        {
            var generator = GeneratorUtilities.GetKeyPairGenerator("ECDSA");
            var generatorParams = new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256r1, new SecureRandom());
            generator.Init(generatorParams);
            var keyPair = generator.GenerateKeyPair();
            return keyPair;
        }
    }
}

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Certes.Jws
{
    internal sealed class RS256 : JsonWebAlgorithm
    {
        public override JsonWebKey JsonWebKey
        {
            get
            {
                var parameters = (RsaKeyParameters)keyPair.Public;
                return new RsaJsonWebKey
                {
                    Exponent = JwsConvert.ToBase64String(parameters.Exponent.ToByteArrayUnsigned()),
                    KeyType = "RSA",
                    Modulus = JwsConvert.ToBase64String(parameters.Modulus.ToByteArrayUnsigned())
                };
            }
        }

        protected override string SigningAlgorithm => "SHA-256withRSA";

        protected override string HashAlgorithm => "SHA256";

        public RS256(AsymmetricCipherKeyPair keyPair)
            : base(keyPair)
        {
        }

        public static AsymmetricCipherKeyPair CreateKeyPair()
        {
            var generator = GeneratorUtilities.GetKeyPairGenerator("RSA");
            var generatorParams = new RsaKeyGenerationParameters(
                BigInteger.ValueOf(0x10001), new SecureRandom(), 2048, 128);
            generator.Init(generatorParams);
            var keyPair = generator.GenerateKeyPair();
            return keyPair;
        }
    }
}

using System;
using System.IO;
using Certes.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Certes.Crypto
{
    internal sealed class RS256SignatureAlgorithm : ISignatureAlgorithm
    {
        public ISigner CreateSigner(ISignatureKey key) => new RS256Signer(key);

        public ISignatureKey GenerateKey()
        {
            var generator = GeneratorUtilities.GetKeyPairGenerator("RSA");
            var generatorParams = new RsaKeyGenerationParameters(
                BigInteger.ValueOf(0x10001), new SecureRandom(), 2048, 128);
            generator.Init(generatorParams);
            var keyPair = generator.GenerateKeyPair();
            return new AsymmetricCipherSignatureKey(SignatureAlgorithm.RS256, keyPair);
        }

        public ISignatureKey ReadKey(Stream data)
        {
            var keyParam = PrivateKeyFactory.CreateKey(data) as RsaPrivateCrtKeyParameters;

            if (keyParam == null)
            {
                throw new NotSupportedException("Unsupported key.");
            }
            else
            {
                var publicKey = new RsaKeyParameters(false, keyParam.Modulus, keyParam.PublicExponent);
                return new AsymmetricCipherSignatureKey(
                    SignatureAlgorithm.RS256, new AsymmetricCipherKeyPair(publicKey, keyParam));
            }
        }
    }
}

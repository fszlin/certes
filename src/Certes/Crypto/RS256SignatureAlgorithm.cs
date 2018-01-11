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
    }
}

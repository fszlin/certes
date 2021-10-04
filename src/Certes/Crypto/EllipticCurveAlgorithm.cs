using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Certes.Crypto
{
    internal sealed class EllipticCurveAlgorithm : IKeyAlgorithm
    {
        private readonly string curveName;
        private readonly string signingAlgorithm;
        private readonly string hashAlgorithm;

        private KeyAlgorithm Algorithm
        {
            get
            {
                switch (curveName)
                {
                    case "P-256": return KeyAlgorithm.ES256;
                    case "P-384": return KeyAlgorithm.ES384;
                    default: return KeyAlgorithm.ES512;
                }
            }
        }

        public EllipticCurveAlgorithm(string curveName, string signingAlgorithm, string hashAlgorithm)
        {
            this.curveName = curveName;
            this.signingAlgorithm = signingAlgorithm;
            this.hashAlgorithm = hashAlgorithm;
        }

        public ISigner CreateSigner(IKey key) => new EllipticCurveSigner(key, signingAlgorithm, hashAlgorithm);

        public IKey GenerateKey(int? keySize = null)
        {
            var generator = GeneratorUtilities.GetKeyPairGenerator("ECDSA");
            var generatorParams = new ECKeyGenerationParameters(
                CustomNamedCurves.GetOid(curveName), new SecureRandom());
            generator.Init(generatorParams);
            var keyPair = generator.GenerateKeyPair();
            return new AsymmetricCipherKey(Algorithm, keyPair);
        }
    }

}

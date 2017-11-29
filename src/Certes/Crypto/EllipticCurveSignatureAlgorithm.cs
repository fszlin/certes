using System;
using System.IO;
using Certes.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.EC;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Certes.Crypto
{
    internal sealed class EllipticCurveSignatureAlgorithm : ISignatureAlgorithm
    {
        private readonly string curveName;
        private readonly string signingAlgorithm;
        private readonly string hashAlgorithm;

        private SignatureAlgorithm Algorithm
        {
            get
            {
                switch (curveName)
                {
                    case "P-256": return SignatureAlgorithm.ES256;
                    case "P-384": return SignatureAlgorithm.ES384;
                    default: return SignatureAlgorithm.ES512;
                }
            }
        }

        public EllipticCurveSignatureAlgorithm(string curveName, string signingAlgorithm, string hashAlgorithm)
        {
            this.curveName = curveName;
            this.signingAlgorithm = signingAlgorithm;
            this.hashAlgorithm = hashAlgorithm;
        }

        public ISigner CreateSigner(ISignatureKey key) => new EllipticCurveSigner(key, signingAlgorithm, hashAlgorithm);

        public ISignatureKey GenerateKey()
        {
            var generator = GeneratorUtilities.GetKeyPairGenerator("ECDSA");
            var generatorParams = new ECKeyGenerationParameters(
                CustomNamedCurves.GetOid(curveName), new SecureRandom());
            generator.Init(generatorParams);
            var keyPair = generator.GenerateKeyPair();
            return new AsymmetricCipherSignatureKey(Algorithm, keyPair);
        }

        public ISignatureKey ReadKey(Stream data)
        {
            var keyParam = PrivateKeyFactory.CreateKey(data) as ECPrivateKeyParameters;

            if (keyParam == null)
            {
                throw new NotSupportedException("Unsupported key.");
            }
            else
            {
                var domain = keyParam.Parameters;
                var q = domain.G.Multiply(keyParam.D);
                var publicKey = new ECPublicKeyParameters(q, domain);
                return new AsymmetricCipherSignatureKey(
                    Algorithm, new AsymmetricCipherKeyPair(publicKey, keyParam));
            }
        }
    }

}

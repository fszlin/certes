using System;
using System.IO;
using Certes.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Certes.Crypto
{
    internal class SignatureAlgorithmProvider
    {
        public ISignatureAlgorithm Get(SignatureAlgorithm algorithm)
        {
            switch (algorithm)
            {
                case SignatureAlgorithm.ES256:
                    return new EllipticCurveSignatureAlgorithm("P-256", "SHA-256withECDSA", "SHA256");
                case SignatureAlgorithm.ES384:
                    return new EllipticCurveSignatureAlgorithm("P-384", "SHA-384withECDSA", "SHA384");
                case SignatureAlgorithm.ES512:
                    return new EllipticCurveSignatureAlgorithm("P-521", "SHA-512withECDSA", "SHA512");
                case SignatureAlgorithm.RS256:
                    return new RS256SignatureAlgorithm();
            }

            throw new ArgumentException(nameof(algorithm));
        }

        public ISignatureKey GetKey(Stream data)
        {
            var keyParam = PrivateKeyFactory.CreateKey(data);

            if (keyParam is RsaPrivateCrtKeyParameters)
            {
                var privateKey = (RsaPrivateCrtKeyParameters)keyParam;
                var publicKey = new RsaKeyParameters(false, privateKey.Modulus, privateKey.PublicExponent);
                return new AsymmetricCipherSignatureKey(
                    SignatureAlgorithm.RS256, new AsymmetricCipherKeyPair(publicKey, keyParam));
            }
            else if (keyParam is ECPrivateKeyParameters)
            {
                var privateKey = (ECPrivateKeyParameters)keyParam;
                var domain = privateKey.Parameters;
                var q = domain.G.Multiply(privateKey.D);
                var publicKey = new ECPublicKeyParameters(q, domain);

                var algo =
                    domain.Curve.FieldSize == 256 ? SignatureAlgorithm.ES256 :
                    domain.Curve.FieldSize == 384 ? SignatureAlgorithm.ES384 :
                    domain.Curve.FieldSize == 521 ? SignatureAlgorithm.ES512 :
                    throw new NotSupportedException();

                return new AsymmetricCipherSignatureKey(
                    algo, new AsymmetricCipherKeyPair(publicKey, keyParam));
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}

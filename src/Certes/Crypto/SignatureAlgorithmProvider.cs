using System;
using System.Collections.Generic;
using System.IO;
using Certes.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Certes.Crypto
{
    internal class SignatureAlgorithmProvider
    {
        private static readonly Dictionary<SignatureAlgorithm, ISignatureAlgorithm> signatureAlgorithms = new Dictionary<SignatureAlgorithm, ISignatureAlgorithm>()
        {
            { SignatureAlgorithm.ES256, new EllipticCurveSignatureAlgorithm("P-256", "SHA-256withECDSA", "SHA256") },
            { SignatureAlgorithm.ES384, new EllipticCurveSignatureAlgorithm("P-384", "SHA-384withECDSA", "SHA384") },
            { SignatureAlgorithm.ES512, new EllipticCurveSignatureAlgorithm("P-521", "SHA-512withECDSA", "SHA512") },
            { SignatureAlgorithm.RS256, new RS256SignatureAlgorithm() },
        };

        public ISignatureAlgorithm Get(SignatureAlgorithm algorithm) =>
            signatureAlgorithms.TryGetValue(algorithm, out var signer) ? signer : throw new ArgumentException(nameof(algorithm));

        public ISignatureKey GetKey(string pem)
        {
            using (var reader = new StringReader(pem))
            {
                var pemReader = new PemReader(reader);
                var pemKey = pemReader.ReadObject() as AsymmetricCipherKeyPair;
                return ReadKey(pemKey.Private);
            }
        }

        public ISignatureKey GetKey(byte[] der)
        {
            var keyParam = PrivateKeyFactory.CreateKey(der);
            return ReadKey(keyParam);
        }

        private static ISignatureKey ReadKey(AsymmetricKeyParameter keyParam)
        {
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

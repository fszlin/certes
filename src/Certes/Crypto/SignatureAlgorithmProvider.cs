using System;
using System.Collections.Generic;
using System.IO;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
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

        internal (SignatureAlgorithm, AsymmetricCipherKeyPair) GetKeyPair(byte[] der)
        {
            var keyParam = PrivateKeyFactory.CreateKey(der);
            return ParseKey(keyParam);
        }

        private static (SignatureAlgorithm, AsymmetricCipherKeyPair) ParseKey(AsymmetricKeyParameter keyParam)
        {
            if (keyParam is RsaPrivateCrtKeyParameters)
            {
                var privateKey = (RsaPrivateCrtKeyParameters)keyParam;
                var publicKey = new RsaKeyParameters(false, privateKey.Modulus, privateKey.PublicExponent);
                return (SignatureAlgorithm.RS256, new AsymmetricCipherKeyPair(publicKey, keyParam));
            }
            else if (keyParam is ECPrivateKeyParameters privateKey)
            {
                var domain = privateKey.Parameters;
                var q = domain.G.Multiply(privateKey.D);

                DerObjectIdentifier curveId;
                SignatureAlgorithm algo;
                switch(domain.Curve.FieldSize)
                {
                    case 256:
                        curveId = SecObjectIdentifiers.SecP256r1;
                        algo = SignatureAlgorithm.ES256;
                        break;
                    case 384:
                        curveId = SecObjectIdentifiers.SecP384r1;
                        algo = SignatureAlgorithm.ES384;
                        break;
                    case 521:
                        curveId = SecObjectIdentifiers.SecP521r1;
                        algo = SignatureAlgorithm.ES512;
                        break;
                    default:
                        throw new NotSupportedException();
                }

                var publicKey = new ECPublicKeyParameters("EC", q, curveId);
                return (algo, new AsymmetricCipherKeyPair(publicKey, keyParam));
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        private static ISignatureKey ReadKey(AsymmetricKeyParameter keyParam)
        {
            var (algo, keyPair) = ParseKey(keyParam);
            return new AsymmetricCipherSignatureKey(algo, keyPair);
        }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using Certes.Jws;
using Certes.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;

namespace Certes.Crypto
{
    internal class AsymmetricCipherSignatureKey : ISignatureKey
    {
        public JsonWebKey JsonWebKey
        {
            get
            {
                if (Algorithm == SignatureAlgorithm.RS256)
                {
                    var rsaKey = (RsaKeyParameters)KeyPair.Public;
                    return new RsaJsonWebKey
                    {
                        Exponent = JwsConvert.ToBase64String(rsaKey.Exponent.ToByteArrayUnsigned()),
                        KeyType = "RSA",
                        Modulus = JwsConvert.ToBase64String(rsaKey.Modulus.ToByteArrayUnsigned())
                    };
                }
                else
                {
                    var ecKey = (ECPublicKeyParameters)KeyPair.Public;
                    var curve =
                        Algorithm == SignatureAlgorithm.ES256 ? "P-256" :
                        Algorithm == SignatureAlgorithm.ES384 ? "P-384" : "P-521";
                    return new EcJsonWebKey
                    {
                        KeyType = "EC",
                        Curve = curve,
                        X = JwsConvert.ToBase64String(ecKey.Q.AffineXCoord.ToBigInteger().ToByteArrayUnsigned()),
                        Y = JwsConvert.ToBase64String(ecKey.Q.AffineYCoord.ToBigInteger().ToByteArrayUnsigned()),
                    };
                }
            }
        }

        public AsymmetricCipherKeyPair KeyPair { get; }

        public SignatureAlgorithm Algorithm { get; }

        public AsymmetricCipherSignatureKey(SignatureAlgorithm algorithm, AsymmetricCipherKeyPair keyPair)
        {
           KeyPair = keyPair ?? throw new ArgumentNullException(nameof(keyPair));
            Algorithm = algorithm;
        }

        public async Task Save(Stream data)
        {
            var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(KeyPair.Private);
            var der = privateKey.GetDerEncoded();
            await data.WriteAsync(der, 0, der.Length);
        }
    }
}

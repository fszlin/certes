using System;
using System.IO;
using Certes.Jws;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;

namespace Certes.Crypto
{
    internal class AsymmetricCipherKey : IKey
    {
        private static byte[] PadBytes(byte[] bytes, int padding)
        {
            var remainder = padding - bytes.Length;

            if (remainder <= 0)
            {
                return bytes;
            }

            var newArray = new byte[bytes.Length + remainder];

            var startAt = newArray.Length - bytes.Length;
            Array.Copy(bytes, 0, newArray, startAt, bytes.Length);
            return newArray;
        }

        public JsonWebKey JsonWebKey
        {
            get
            {
                if (Algorithm == KeyAlgorithm.RS256)
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
                        Algorithm == KeyAlgorithm.ES256 ? "P-256" :
                        Algorithm == KeyAlgorithm.ES384 ? "P-384" : "P-521";

                    // https://tools.ietf.org/html/rfc7518#section-6.2.1.2
                    // get the byte representation of the x & y coords on the Elliptic Curve,
                    // then pad bytes to the required field length before encoding

                    var xBytes = ecKey.Q.AffineXCoord.ToBigInteger().ToByteArrayUnsigned();
                    var yBytes = ecKey.Q.AffineYCoord.ToBigInteger().ToByteArrayUnsigned();

                    int requiredLength = ecKey.Parameters.Curve.FieldSize / 8;

                    return new EcJsonWebKey
                    {
                        KeyType = "EC",
                        Curve = curve,
                        X = JwsConvert.ToBase64String(PadBytes(xBytes, requiredLength)),
                        Y = JwsConvert.ToBase64String(PadBytes(yBytes, requiredLength))
                    };
                }
            }
        }

        public AsymmetricCipherKeyPair KeyPair { get; }

        public KeyAlgorithm Algorithm { get; }

        public AsymmetricCipherKey(KeyAlgorithm algorithm, AsymmetricCipherKeyPair keyPair)
        {
            KeyPair = keyPair ?? throw new ArgumentNullException(nameof(keyPair));
            Algorithm = algorithm;
        }

        public byte[] ToDer()
        {
            var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(KeyPair.Private);
            return privateKey.GetDerEncoded();
        }

        public string ToPem()
        {
            using (var sr = new StringWriter())
            {
                var pemWriter = new PemWriter(sr);
                pemWriter.WriteObject(KeyPair);
                return sr.ToString();
            }
        }
    }
}

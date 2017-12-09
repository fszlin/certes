using System;
using System.IO;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;

namespace Certes.Pkcs
{
    /// <summary>
    /// Represents a key pair.
    /// </summary>
    public class KeyInfo
    {
        /// <summary>
        /// Gets or sets the private key information.
        /// </summary>
        /// <value>
        /// The private key information.
        /// </value>
        [JsonProperty("der")]
        public byte[] PrivateKeyInfo { get; set; }

        /// <summary>
        /// Reads the key from the given <paramref name="steam"/>.
        /// </summary>
        /// <param name="steam">The steam.</param>
        /// <returns>The key loaded.</returns>
        public static KeyInfo From(Stream steam)
        {
            var keyParam = PrivateKeyFactory.CreateKey(steam);
            var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyParam);
            return new KeyInfo
            {
                PrivateKeyInfo = privateKey.GetDerEncoded()
            };
        }
    }

    /// <summary>
    /// Helper methods for <see cref="KeyInfo"/>.
    /// </summary>
    public static class KeyInfoExtensions
    {
        /// <summary>
        /// Saves the key pair to the specified stream.
        /// </summary>
        /// <param name="keyInfo">The key information.</param>
        /// <param name="stream">The stream.</param>
        public static void Save(this KeyInfo keyInfo, Stream stream)
        {
            var keyPair = keyInfo.CreateKeyPair();
            using (var writer = new StreamWriter(stream))
            {
                var pemWriter = new PemWriter(writer);
                pemWriter.WriteObject(keyPair);
            }
        }

        internal static AsymmetricCipherKeyPair CreateKeyPair(this KeyInfo keyInfo)
        {
            var keyParam = PrivateKeyFactory.CreateKey(keyInfo.PrivateKeyInfo);

            if (keyParam is RsaPrivateCrtKeyParameters)
            {
                var privateKey = (RsaPrivateCrtKeyParameters)keyParam;
                var publicKey = new RsaKeyParameters(false, privateKey.Modulus, privateKey.PublicExponent);
                return new AsymmetricCipherKeyPair(publicKey, keyParam);
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

                return new AsymmetricCipherKeyPair(publicKey, keyParam);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        internal static KeyInfo Export(this AsymmetricCipherKeyPair keyPair)
        {
            var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);

            return new KeyInfo
            {
                PrivateKeyInfo = privateKey.GetDerEncoded()
            };
        }
    }
}

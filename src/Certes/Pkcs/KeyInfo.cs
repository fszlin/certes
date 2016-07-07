using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using System;
using System.IO;

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
                return new AsymmetricCipherKeyPair(publicKey, privateKey);
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

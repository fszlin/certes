using System;
using System.IO;
using Certes.Crypto;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;

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
        /// Reads the key from the given <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The steam.</param>
        /// <returns>The key loaded.</returns>
        public static KeyInfo From(Stream stream)
        {
            using (var streamReader = new StreamReader(stream))
            {
                var reader = new PemReader(streamReader);
                var keyPair = reader.ReadObject() as AsymmetricCipherKeyPair;

                if (keyPair == null)
                {
                    throw new Exception("Invaid key data.");
                }

                return keyPair.Export();
            }
        }
    }

    /// <summary>
    /// Helper methods for <see cref="KeyInfo"/>.
    /// </summary>
    public static class KeyInfoExtensions
    {
        private static readonly SignatureAlgorithmProvider signatureAlgorithmProvider = new SignatureAlgorithmProvider();

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
            var (_, keyPair) = signatureAlgorithmProvider.GetKeyPair(keyInfo.PrivateKeyInfo);
            return keyPair;
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

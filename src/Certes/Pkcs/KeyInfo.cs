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
    public class KeyInfo
    {
        [JsonProperty("der")]
        public byte[] PrivateKeyInfo { get; set; }
    }

    public static class KeyInfoExtensions
    {
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

        public static void Save(this KeyInfo keyInfo, Stream stream)
        {
            var keyPair = keyInfo.CreateKeyPair();
            using (var writer = new StreamWriter(stream))
            {
                var pemWriter = new PemWriter(writer);
                pemWriter.WriteObject(keyPair);
            }
        }
    }
}

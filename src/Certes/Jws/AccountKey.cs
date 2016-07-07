using Certes.Pkcs;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;

namespace Certes.Jws
{
    /// <summary>
    /// Represents a JSON Web Signature (JWS) key pair.
    /// </summary>
    /// <seealso cref="Certes.Jws.IAccountKey" />
    public class AccountKey : IAccountKey
    {
        private AsymmetricCipherKeyPair keyPair;
        private object jwk;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountKey"/> class.
        /// </summary>
        /// <param name="keyInfo">The key information.</param>
        /// <exception cref="System.NotSupportedException">
        /// If the provided key is not one of the supported <seealso cref="SignatureAlgorithm"/>.
        /// </exception>
        public AccountKey(KeyInfo keyInfo = null)
        {
            if (keyInfo == null)
            {
                this.keyPair = SignatureAlgorithm.Sha256WithRsaEncryption.Create();
                this.Algorithm = SignatureAlgorithm.Sha256WithRsaEncryption;
            }
            else
            {
                this.keyPair = keyInfo.CreateKeyPair();
                if (this.keyPair.Private is RsaPrivateCrtKeyParameters)
                {
                    this.Algorithm = SignatureAlgorithm.Sha256WithRsaEncryption;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// Gets the signing algorithm.
        /// </summary>
        /// <value>
        /// The signing algorithm.
        /// </value>
        public SignatureAlgorithm Algorithm { get; }

        /// <summary>
        /// Gets the JSON web key.
        /// </summary>
        /// <value>
        /// The JSON web key.
        /// </value>
        public object Jwk
        {
            get
            {
                if (jwk != null)
                {
                    return jwk;
                }

                var parameters = (RsaPrivateCrtKeyParameters)keyPair.Private;
                return jwk = new JsonWebKey
                {
                    Exponent = JwsConvert.ToBase64String(parameters.PublicExponent.ToByteArrayUnsigned()),
                    KeyType = "RSA",
                    Modulus = JwsConvert.ToBase64String(parameters.Modulus.ToByteArrayUnsigned())
                };
            }
        }

        /// <summary>
        /// Computes the hash for given data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The hash.</returns>
        public byte[] ComputeHash(byte[] data)
        {
            var sha256 = new Sha256Digest();
            var hashed = new byte[sha256.GetDigestSize()];

            sha256.BlockUpdate(data, 0, data.Length);
            sha256.DoFinal(hashed, 0);

            return hashed;
        }

        /// <summary>
        /// Signs the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The signature.</returns>
        public byte[] SignData(byte[] data)
        {
            var signer = SignerUtilities.GetSigner(PkcsObjectIdentifiers.Sha256WithRsaEncryption);
            signer.Init(true, keyPair.Private);
            signer.BlockUpdate(data, 0, data.Length);
            var signature = signer.GenerateSignature();
            return signature;
        }

        /// <summary>
        /// Exports the key pair.
        /// </summary>
        /// <returns>The key pair.</returns>
        public KeyInfo Export()
        {
            return this.keyPair.Export();
        }

        private class JsonWebKey
        {
            [JsonProperty("e", Order = 0)]
            public string Exponent { get; set; }

            [JsonProperty("kty", Order = 1)]
            public string KeyType { get; set; }

            [JsonProperty("n", Order = 2)]
            public string Modulus { get; set; }
        }
    }
}

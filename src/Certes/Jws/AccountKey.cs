using System;
using System.IO;
using Certes.Crypto;
using Certes.Pkcs;

namespace Certes.Jws
{
    /// <summary>
    /// Represents a JSON Web Signature (JWS) key pair.
    /// </summary>
    /// <seealso cref="Certes.Jws.IAccountKey" />
    public class AccountKey : IAccountKey
    {
        private static readonly SignatureAlgorithmProvider signatureAlgorithmProvider = new SignatureAlgorithmProvider();
        
        private JsonWebKey jwk;
        private readonly ISignatureAlgorithm signatureAlgorithm;
        private readonly ISignatureKey signatureKey;
        private readonly ISigner signer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountKey"/> class.
        /// </summary>
        /// <param name="algorithm">The JWS signature algorithm.</param>
        public AccountKey(SignatureAlgorithm algorithm = SignatureAlgorithm.ES256)
        {
            signatureAlgorithm = signatureAlgorithmProvider.Get(algorithm);
            signatureKey = signatureAlgorithm.GenerateKey();
            signer = signatureAlgorithm.CreateSigner(signatureKey);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountKey" /> class.
        /// </summary>
        /// <param name="keyInfo">The key information.</param>
        /// <exception cref="ArgumentNullException">keyInfo</exception>
        /// <exception cref="NotSupportedException">
        /// If the provided key is not one of the supported <seealso cref="SignatureAlgorithm" />.
        /// </exception>
        public AccountKey(KeyInfo keyInfo)
        {
            if (keyInfo == null)
            {
                throw new ArgumentNullException(nameof(keyInfo));
            }

            using (var buffer = new MemoryStream(keyInfo.PrivateKeyInfo))
            {
                signatureKey = signatureAlgorithmProvider.GetKey(buffer);
            }

            signatureAlgorithm = signatureAlgorithmProvider.Get(signatureKey.Algorithm);
            signer = signatureAlgorithm.CreateSigner(signatureKey);
        }

        /// <summary>
        /// Gets the signing algorithm.
        /// </summary>
        /// <value>
        /// The signing algorithm.
        /// </value>
        public SignatureAlgorithm Algorithm
        {
            get
            {
                return signatureKey.Algorithm;
            }
        }

        /// <summary>
        /// Gets the JSON web key.
        /// </summary>
        /// <value>
        /// The JSON web key.
        /// </value>
        [Obsolete]
        public object Jwk => JsonWebKey;

        /// <summary>
        /// Gets the JSON web key.
        /// </summary>
        /// <value>
        /// The JSON web key.
        /// </value>
        public JsonWebKey JsonWebKey => jwk ?? (jwk = signatureKey.JsonWebKey);

        /// <summary>
        /// Computes the hash for given data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The hash.</returns>
        public byte[] ComputeHash(byte[] data) => signer.ComputeHash(data);

        /// <summary>
        /// Signs the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The signature.</returns>
        public byte[] SignData(byte[] data) => signer.SignData(data);

        /// <summary>
        /// Exports the key pair.
        /// </summary>
        /// <returns>The key pair.</returns>
        public KeyInfo Export()
        {
            using (var buffer = new MemoryStream())
            {
                signatureKey.Save(buffer);
                return new KeyInfo
                {
                    PrivateKeyInfo = buffer.ToArray()
                };
            }   
        }
    }
}

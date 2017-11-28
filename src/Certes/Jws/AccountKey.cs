using System;
using Certes.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;

namespace Certes.Jws
{
    /// <summary>
    /// Represents a JSON Web Signature (JWS) key pair.
    /// </summary>
    /// <seealso cref="Certes.Jws.IAccountKey" />
    public class AccountKey : IAccountKey
    {
        private readonly AsymmetricCipherKeyPair keyPair;
        private JsonWebKey jwk;

        private Lazy<IJsonWebAlgorithm> jwa;
        
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
                this.keyPair = SignatureAlgorithm.RS256.CreateKeyPair();
                this.Algorithm = SignatureAlgorithm.RS256;
            }
            else
            {
                this.keyPair = keyInfo.CreateKeyPair();
                if (this.keyPair.Private is RsaPrivateCrtKeyParameters)
                {
                    this.Algorithm = SignatureAlgorithm.RS256;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }

            this.jwa = new Lazy<IJsonWebAlgorithm>(() => Algorithm.CreateJwa(keyPair));
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
        [Obsolete]
        public object Jwk
        {
            get
            {
                return JsonWebKey;
            }
        }

        /// <summary>
        /// Gets the JSON web key.
        /// </summary>
        /// <value>
        /// The JSON web key.
        /// </value>
        public JsonWebKey JsonWebKey
        {
            get
            {
                if (jwk != null)
                {
                    return jwk;
                }

                return jwk = jwa.Value.JsonWebKey;
            }
        }

        /// <summary>
        /// Computes the hash for given data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The hash.</returns>
        public byte[] ComputeHash(byte[] data) => jwa.Value.ComputeHash(data);

        /// <summary>
        /// Signs the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The signature.</returns>
        public byte[] SignData(byte[] data) => jwa.Value.SignData(data);

        /// <summary>
        /// Exports the key pair.
        /// </summary>
        /// <returns>The key pair.</returns>
        public KeyInfo Export()
        {
            return this.keyPair.Export();
        }
    }
}

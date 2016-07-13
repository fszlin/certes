using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using System;

namespace Certes.Pkcs
{
    /// <summary>
    /// Represents a CSR builder.
    /// </summary>
    /// <seealso cref="Certes.Pkcs.CertificationRequestBuilderBase" />
    public class CertificationRequestBuilder : CertificationRequestBuilderBase
    {
        private KeyInfo keyInfo;

        /// <summary>
        /// Gets the algorithm.
        /// </summary>
        /// <value>
        /// The algorithm.
        /// </value>
        protected override SignatureAlgorithm Algorithm { get; }

        /// <summary>
        /// Gets the key pair.
        /// </summary>
        /// <value>
        /// The key pair.
        /// </value>
        protected override AsymmetricCipherKeyPair KeyPair { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificationRequestBuilder"/> class.
        /// </summary>
        /// <param name="keyInfo">The key information.</param>
        public CertificationRequestBuilder(KeyInfo keyInfo)
        {
            this.keyInfo = keyInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificationRequestBuilder"/> class.
        /// </summary>
        /// <exception cref="System.NotSupportedException">
        /// If the provided key is not one of the supported <seealso cref="SignatureAlgorithm"/>.
        /// </exception>
        public CertificationRequestBuilder()
        {
            if (keyInfo == null)
            {
                this.KeyPair = SignatureAlgorithm.Sha256WithRsaEncryption.Create();
                this.Algorithm = SignatureAlgorithm.Sha256WithRsaEncryption;
            }
            else
            {
                this.KeyPair = keyInfo.CreateKeyPair();
                if (this.KeyPair.Private is RsaPrivateCrtKeyParameters)
                {
                    this.Algorithm = SignatureAlgorithm.Sha256WithRsaEncryption;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
    }

}

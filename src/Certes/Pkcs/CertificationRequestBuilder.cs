using System;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Certes.Pkcs
{
    /// <summary>
    /// Represents a CSR builder.
    /// </summary>
    /// <seealso cref="Certes.Pkcs.CertificationRequestBuilderBase" />
    public class CertificationRequestBuilder : CertificationRequestBuilderBase
    {
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
        /// <exception cref="System.NotSupportedException">
        /// If the provided key is not one of the supported <seealso cref="SignatureAlgorithm"/>.
        /// </exception>
        public CertificationRequestBuilder(KeyInfo keyInfo)
        {
            var keyParam = PrivateKeyFactory.CreateKey(keyInfo.PrivateKeyInfo);

            if (keyParam is RsaPrivateCrtKeyParameters)
            {
                var privateKey = (RsaPrivateCrtKeyParameters)keyParam;
                var publicKey = new RsaKeyParameters(false, privateKey.Modulus, privateKey.PublicExponent);
                KeyPair = new AsymmetricCipherKeyPair(publicKey, keyParam);
                Algorithm = SignatureAlgorithm.RS256;
            }
            else
            {
                throw new NotSupportedException("Unsupported key");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificationRequestBuilder"/> class.
        /// </summary>
        public CertificationRequestBuilder()
        {
            var generator = GeneratorUtilities.GetKeyPairGenerator("RSA");
            var generatorParams = new RsaKeyGenerationParameters(
                BigInteger.ValueOf(0x10001), new SecureRandom(), 2048, 128);
            generator.Init(generatorParams);
            KeyPair = generator.GenerateKeyPair();
            Algorithm = SignatureAlgorithm.RS256;
        }
    }

}

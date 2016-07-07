using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using System;

namespace Certes.Pkcs
{
    /// <summary>
    /// The supported algorithms.
    /// </summary>
    public enum SignatureAlgorithm
    {
        /// <summary>
        /// SHA256 hash with RSA encryption.
        /// </summary>
        Sha256WithRsaEncryption
    }

    /// <summary>
    /// Helper methods for <see cref="SignatureAlgorithm"/>.
    /// </summary>
    public static class SignatureAlgorithmExtensions
    {
        /// <summary>
        /// Get the JWS name of the <paramref name="algorithm"/>.
        /// </summary>
        /// <param name="algorithm">The algorithm.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">If the <paramref name="algorithm"/> is not supported.</exception>
        public static string ToJwsAlgorithm(this SignatureAlgorithm algorithm)
        {
            if (algorithm == SignatureAlgorithm.Sha256WithRsaEncryption)
            {
                return "RS256";
            }

            throw new ArgumentException(nameof(algorithm));
        }

        internal static AsymmetricCipherKeyPair Create(this SignatureAlgorithm algo)
        {
            var generator = new RsaKeyPairGenerator();
            var generatorParams = new RsaKeyGenerationParameters(BigInteger.ValueOf(0x10001), new SecureRandom(), 2048, 128);
            generator.Init(generatorParams);
            var keyPair = generator.GenerateKeyPair();
            return keyPair;
        }

        internal static string ToPkcsObjectId(this SignatureAlgorithm algo)
        {
            switch (algo)
            {
                case SignatureAlgorithm.Sha256WithRsaEncryption:
                    return PkcsObjectIdentifiers.Sha256WithRsaEncryption.Id;
            }

            return null;
        }
    }
}

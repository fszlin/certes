using System;
using Certes.Jws;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;

namespace Certes.Pkcs
{
    /// <summary>
    /// The supported algorithms.
    /// </summary>
    public enum SignatureAlgorithm
    {
        /// <summary>
        /// RSASSA-PKCS1-v1_5 using SHA-256.
        /// </summary>
        RS256,

        /// <summary>
        /// ECDSA using P-256 and SHA-256.
        /// </summary>
        ES256,

        /// <summary>
        /// SHA256 hash with RSA encryption.
        /// </summary>
        [Obsolete("Use RS256 instead.")]
        Sha256WithRsaEncryption = RS256,
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
            var algo = algorithm.ToString();

            if (string.IsNullOrEmpty(algo))
            {
                throw new ArgumentException(nameof(algorithm));
            }

            return algo;
        }

        internal static AsymmetricCipherKeyPair CreateKeyPair(this SignatureAlgorithm algo)
        {
            switch (algo)
            {
                case SignatureAlgorithm.RS256:
                    return RS256.CreateKeyPair();
                case SignatureAlgorithm.ES256:
                    return ES256.CreateKeyPair();
            }
            
            throw new ArgumentException(nameof(algo));
        }

        internal static IJsonWebAlgorithm CreateJwa(this SignatureAlgorithm algo, AsymmetricCipherKeyPair keyPair)
        {
            switch (algo)
            {
                case SignatureAlgorithm.RS256:
                    return new RS256(keyPair);
                case SignatureAlgorithm.ES256:
                    return new ES256(keyPair);
            }

            throw new ArgumentException(nameof(algo));
        }

        internal static string ToPkcsObjectId(this SignatureAlgorithm algo)
        {
            switch (algo)
            {
                case SignatureAlgorithm.RS256:
                    return PkcsObjectIdentifiers.Sha256WithRsaEncryption.Id;
            }

            return null;
        }
    }
}

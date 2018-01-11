using System;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X9;

namespace Certes
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
        /// ECDSA using P-384 and SHA-384.
        /// </summary>
        ES384,

        /// <summary>
        /// ECDSA using P-521 and SHA-512.
        /// </summary>
        ES512,

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
        public static string ToJwsAlgorithm(this SignatureAlgorithm algorithm)
        {
            if (!Enum.IsDefined(typeof(SignatureAlgorithm), algorithm))
            {
                throw new ArgumentException(nameof(algorithm));
            }

            return algorithm.ToString();
        }

        internal static string ToPkcsObjectId(this SignatureAlgorithm algo)
        {
            switch (algo)
            {
                case SignatureAlgorithm.RS256:
                    return PkcsObjectIdentifiers.Sha256WithRsaEncryption.Id;
                case SignatureAlgorithm.ES256:
                    return X9ObjectIdentifiers.ECDsaWithSha256.Id;
                case SignatureAlgorithm.ES384:
                    return X9ObjectIdentifiers.ECDsaWithSha384.Id;
                case SignatureAlgorithm.ES512:
                    return X9ObjectIdentifiers.ECDsaWithSha512.Id;
            }

            return null;
        }
    }
}

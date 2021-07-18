using System;
using System.Text;
using Certes.Crypto;
using Certes.Json;
using Certes.Pkcs;
using Newtonsoft.Json;
using Org.BouncyCastle.Security;

namespace Certes.Jws
{
    /// <summary>
    /// Represents a JSON Web Signature (JWS) key pair.
    /// </summary>
    public interface IAccountKey
    {
        /// <summary>
        /// Gets the signing algorithm.
        /// </summary>
        /// <value>
        /// The signing algorithm.
        /// </value>
        KeyAlgorithm Algorithm { get; }

        /// <summary>
        /// Signs the data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The signature.</returns>
        byte[] SignData(byte[] data);

        /// <summary>
        /// Computes the hash for given data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The hash.</returns>
        byte[] ComputeHash(byte[] data);

        /// <summary>
        /// Gets the JSON web key.
        /// </summary>
        /// <value>
        /// The JSON web key.
        /// </value>
        JsonWebKey JsonWebKey { get; }

        /// <summary>
        /// Gets the signature key.
        /// </summary>
        /// <value>
        /// The signature key.
        /// </value>
        IKey SignatureKey { get; }

        /// <summary>
        /// Exports the key pair.
        /// </summary>
        /// <returns>The key pair.</returns>
        KeyInfo Export();
    }

    /// <summary>
    /// Helper methods for <see cref="AccountKey"/>.
    /// </summary>
    public static class AccountKeyExtensions
    {
        private static readonly JsonSerializerSettings thumbprintSettings = JsonUtil.CreateSettings();

        /// <summary>
        /// Generates the thumbprint for the given account <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The account key.</param>
        /// <returns>The thumbprint.</returns>
        public static byte[] GenerateThumbprint(this IAccountKey key) => key.SignatureKey.GenerateThumbprint();

        /// <summary>
        /// Generates the base64 encoded thumbprint for the given account <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The account key.</param>
        /// <returns>The thumbprint.</returns>
        public static string Thumbprint(this IAccountKey key) => key.SignatureKey.Thumbprint();

        /// <summary>
        /// Generates key authorization string.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="token">The challenge token.</param>
        /// <returns></returns>
        public static string KeyAuthorization(this IAccountKey key, string token) => $"{token}.{key.Thumbprint()}";
    }
}

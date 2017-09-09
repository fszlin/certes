using Certes.Pkcs;
using Newtonsoft.Json;
using System;
using System.Text;

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
        SignatureAlgorithm Algorithm { get; }

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
        [Obsolete("Use JsonWebKey instead.")]
        object Jwk { get; }

        /// <summary>
        /// Gets the JSON web key.
        /// </summary>
        /// <value>
        /// The JSON web key.
        /// </value>
        JsonWebKey JsonWebKey { get; }

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
        private static readonly JsonSerializerSettings thumbprintSettings = new JsonSerializerSettings();

        /// <summary>
        /// Generates the thumbprint for the given account <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The account key.</param>
        /// <returns>The thumbprint.</returns>
        public static byte[] GenerateThumbprint(this IAccountKey key)
        {
            var jwk = key.JsonWebKey;
            var json = JsonConvert.SerializeObject(jwk, Formatting.None, thumbprintSettings);
            var bytes = Encoding.UTF8.GetBytes(json);
            var hashed = key.ComputeHash(bytes);

            return hashed;
        }

        /// <summary>
        /// Generates the base64 encoded thumbprint for the given account <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The account key.</param>
        /// <returns>The thumbprint.</returns>
        public static string Thumbprint(this IAccountKey key)
        {
            var jwkThumbprint = key.GenerateThumbprint();
            return JwsConvert.ToBase64String(jwkThumbprint);
        }
    }
}

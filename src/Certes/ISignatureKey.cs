using System.Text;
using Certes.Json;
using Certes.Jws;
using Certes.Pkcs;
using Newtonsoft.Json;
using Org.BouncyCastle.Security;

namespace Certes
{
    /// <summary>
    /// Represents key parameters used for signing.
    /// </summary>
    /// <remarks>
    /// May add netcore implementations once the API is ready - https://github.com/dotnet/designs/issues/11.
    /// </remarks>
    public interface ISignatureKey
    {
        /// <summary>
        /// Gets the algorithm.
        /// </summary>
        /// <value>
        /// The algorithm.
        /// </value>
        SignatureAlgorithm Algorithm { get;}

        /// <summary>
        /// Gets the json web key.
        /// </summary>
        /// <value>
        /// The json web key.
        /// </value>
        JsonWebKey JsonWebKey { get; }

        /// <summary>
        /// Exports the key pair to DER.
        /// </summary>
        /// <returns>The DER encoded key pair data.</returns>
        byte[] ToDer();

        /// <summary>
        /// Exports the key pair to PEM.
        /// </summary>
        /// <returns>The key pair data.</returns>
        string ToPem();
    }

    /// <summary>
    /// Helper methods for <see cref="AccountKey"/>.
    /// </summary>
    public static class ISignatureKeyExtensions
    {
        private static readonly JsonSerializerSettings thumbprintSettings = JsonUtil.CreateSettings();

        /// <summary>
        /// Generates the thumbprint for the given account <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The account key.</param>
        /// <returns>The thumbprint.</returns>
        private static byte[] GenerateThumbprint(this ISignatureKey key)
        {
            var jwk = key.JsonWebKey;
            var json = JsonConvert.SerializeObject(jwk, Formatting.None, thumbprintSettings);
            var bytes = Encoding.UTF8.GetBytes(json);
            var hashed = key.GetSigner().ComputeHash(bytes);

            return hashed;
        }

        /// <summary>
        /// Generates the base64 encoded thumbprint for the given account <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The account key.</param>
        /// <returns>The thumbprint.</returns>
        public static string Thumbprint(this ISignatureKey key)
        {
            var jwkThumbprint = key.GenerateThumbprint();
            return JwsConvert.ToBase64String(jwkThumbprint);
        }

        /// <summary>
        /// Generates key authorization string.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="token">The challenge token.</param>
        /// <returns></returns>
        public static string KeyAuthorization(this ISignatureKey key, string token)
        {
            var jwkThumbprintEncoded = key.Thumbprint();
            return $"{token}.{jwkThumbprintEncoded}";
        }

        /// <summary>
        /// Generates the value for DNS TXT record.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="token">The challenge token.</param>
        /// <returns></returns>
        public static string DnsTxtRecord(this ISignatureKey key, string token)
        {
            var keyAuthz = key.KeyAuthorization(token);
            var hashed = DigestUtilities.CalculateDigest("SHA256", Encoding.UTF8.GetBytes(keyAuthz));
            return JwsConvert.ToBase64String(hashed);
        }
    }
}

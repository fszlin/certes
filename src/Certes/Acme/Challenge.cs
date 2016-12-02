using Certes.Jws;
using Org.BouncyCastle.Crypto.Digests;
using System;
using System.Collections.Generic;
using System.Text;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the ACME Challenge entity.
    /// </summary>
    /// <seealso cref="Certes.Acme.EntityBase" />
    public class Challenge : EntityBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Challenge"/> class.
        /// </summary>
        public Challenge()
        {
            this.Resource = ResourceTypes.Challenge;
        }
        /// <summary>
        /// Gets or sets the challenge type.
        /// </summary>
        /// <value>
        /// The challenge type.
        /// </value>
        /// <seealso cref="ChallengeTypes"/>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        /// <value>
        /// The URI.
        /// </value>
        public Uri Uri { get; set; }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        /// <seealso cref="EntityStatus"/>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the validated date.
        /// </summary>
        /// <value>
        /// The validated date.
        /// </value>
        public DateTimeOffset? Validated { get; set; }

        /// <summary>
        /// Gets or sets the key authorization string.
        /// </summary>
        /// <value>
        /// The key authorization string.
        /// </value>
        public string KeyAuthorization { get; set; }

        /// <summary>
        /// Gets or sets the validation records.
        /// </summary>
        /// <value>
        /// The validation records.
        /// </value>
        public IList<ChallengeValidation> ValidationRecord { get; set; }
    }

    /// <summary>
    /// Helper methods for <see cref="Challenge"/>.
    /// </summary>
    public static class ChallengeExtensions
    {
        /// <summary>
        /// Computes the key authorization string for <paramref name="challenge"/>.
        /// </summary>
        /// <param name="challenge">The challenge.</param>
        /// <param name="key">The key.</param>
        /// <returns>The key authorization string.</returns>
        public static string ComputeKeyAuthorization(this Challenge challenge, IAccountKey key)
        {
            var jwkThumbprint = key.GenerateThumbprint();
            var jwkThumbprintEncoded = JwsConvert.ToBase64String(jwkThumbprint);
            var token = challenge.Token;
            return $"{token}.{jwkThumbprintEncoded}";
        }

        /// <summary>
        /// Computes the DNS value for the <paramref name="challenge"/>.
        /// </summary>
        /// <param name="challenge">The challenge.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value for the text DNS record.</returns>
        public static string ComputeDnsValue(this Challenge challenge, IAccountKey key)
        {
            var keyAuthString = challenge.ComputeKeyAuthorization(key);
            var keyAuthBytes = Encoding.UTF8.GetBytes(keyAuthString);
            var sha256 = new Sha256Digest();
            var hashed = new byte[sha256.GetDigestSize()];

            sha256.BlockUpdate(keyAuthBytes, 0, keyAuthBytes.Length);
            sha256.DoFinal(hashed, 0);

            var dnsValue = JwsConvert.ToBase64String(hashed);
            return dnsValue;
        }
    }
}

using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents a challenge for <see cref="Identifier"/>.
    /// </summary>
    public class Challenge
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        [JsonPropertyName("url")]
        public Uri Url { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        [JsonPropertyName("status")]
        public ChallengeStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the validation time.
        /// </summary>
        /// <value>
        /// The validation time.
        /// </value>
        [JsonPropertyName("validated")]
        public DateTimeOffset? Validated { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// Only if the status is invalid
        /// </summary>
        /// <value>
        /// The errors.
        /// </value>
        [JsonPropertyName("error")]
        public AcmeError Error { get; set; }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        [JsonPropertyName("token")]
        public string Token { get; set; }
    }
}

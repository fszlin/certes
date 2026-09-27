using System;
using System.Text.Json.Serialization;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the ACME Renewal Information (ARI) resource of a certificate, as defined in RFC 9773.
    /// </summary>
    public class RenewalInfo
    {
        /// <summary>
        /// Gets or sets the window in which the server suggests renewing the certificate.
        /// </summary>
        /// <value>
        /// The suggested renewal window.
        /// </value>
        [JsonPropertyName("suggestedWindow")]
        public SuggestedWindow SuggestedWindow { get; set; }

        /// <summary>
        /// Gets or sets a URL describing the reason for the suggested window.
        /// </summary>
        /// <value>
        /// The explanation URL, or <c>null</c>.
        /// </value>
        [JsonPropertyName("explanationURL")]
        public Uri ExplanationUrl { get; set; }

        /// <summary>
        /// Gets or sets the delay the server requested before this resource is polled again,
        /// from the <c>Retry-After</c> response header.
        /// </summary>
        /// <value>
        /// The retry delay, or <c>null</c> if the server did not send one.
        /// </value>
        [JsonIgnore]
        public TimeSpan? RetryAfter { get; set; }
    }
}

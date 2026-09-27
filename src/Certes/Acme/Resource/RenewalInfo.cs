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
        /// The positive retry delay reported by the HTTP client, or <c>null</c> if none is available.
        /// </value>
        /// <remarks>
        /// This value is not clamped and this API does not schedule polling. Callers must
        /// apply reasonable checking-interval limits (RFC 9773 section 4.3.2), for example
        /// one minute to one day. If no delay is available, use a locally configured
        /// fallback, such as six hours. Error backoff takes priority over polling.
        /// </remarks>
        [JsonIgnore]
        public TimeSpan? RetryAfter { get; set; }
    }
}

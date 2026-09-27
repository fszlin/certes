using System;
using System.Text.Json.Serialization;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the renewal window suggested by an ACME server (RFC 9773).
    /// </summary>
    public class SuggestedWindow
    {
        /// <summary>
        /// Gets or sets the start of the suggested renewal window.
        /// </summary>
        /// <value>
        /// The start of the window.
        /// </value>
        [JsonPropertyName("start")]
        public DateTimeOffset Start { get; set; }

        /// <summary>
        /// Gets or sets the end of the suggested renewal window.
        /// </summary>
        /// <value>
        /// The end of the window.
        /// </value>
        [JsonPropertyName("end")]
        public DateTimeOffset End { get; set; }
    }
}

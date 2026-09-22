using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the ACME Authorization resource.
    /// </summary>
    public class Authorization
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        [JsonPropertyName("identifier")]
        public Identifier Identifier { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        [JsonPropertyName("status")]
        public AuthorizationStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the expires.
        /// </summary>
        /// <value>
        /// The expires.
        /// </value>
        [JsonPropertyName("expires")]
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// Gets or sets the scope.
        /// </summary>
        /// <value>
        /// The scope.
        /// </value>
        [JsonPropertyName("scope")]
        public Uri Scope { get; set; }

        /// <summary>
        /// Gets or sets the challenges.
        /// </summary>
        /// <value>
        /// The challenges.
        /// </value>
        [JsonPropertyName("challenges")]
        public IList<Challenge> Challenges { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating if this authorization is for wildcard.
        /// </summary>
        /// <value>
        /// The flag indicating if this authorization is for wildcard.
        /// </value>
        [JsonPropertyName("wildcard")]
        public bool? Wildcard { get; set; }

    }
}

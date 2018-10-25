using Newtonsoft.Json;
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
        [JsonProperty("identifier")]
        public Identifier Identifier { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        [JsonProperty("status")]
        public AuthorizationStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the expires.
        /// </summary>
        /// <value>
        /// The expires.
        /// </value>
        [JsonProperty("expires")]
        public DateTimeOffset? Expires { get; set; }

        /// <summary>
        /// Gets or sets the scope.
        /// </summary>
        /// <value>
        /// The scope.
        /// </value>
        [JsonProperty("scope")]
        public Uri Scope { get; set; }

        /// <summary>
        /// Gets or sets the challenges.
        /// </summary>
        /// <value>
        /// The challenges.
        /// </value>
        [JsonProperty("challenges")]
        public IList<Challenge> Challenges { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating if this authorization is for wildcard.
        /// </summary>
        /// <value>
        /// The flag indicating if this authorization is for wildcard.
        /// </value>
        [JsonProperty("wildcard")]
        public bool? Wildcard { get; set; }

    }
}

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the metadata for a ACME directory.
    /// </summary>
    public class DirectoryMeta
    {
        /// <summary>
        /// Gets or sets the terms of service.
        /// </summary>
        /// <value>
        /// The terms of service.
        /// </value>
        [JsonProperty("termsOfService")]
        public Uri TermsOfService { get; set; }

        /// <summary>
        /// Gets or sets the website.
        /// </summary>
        /// <value>
        /// The website.
        /// </value>
        [JsonProperty("website")]
        public Uri Website { get; set; }

        /// <summary>
        /// Gets or sets the caa identities.
        /// </summary>
        /// <value>
        /// The caa identities.
        /// </value>
        [JsonProperty("caaIdentities")]
        public IReadOnlyList<string> CaaIdentities { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [external account required].
        /// </summary>
        /// <value>
        ///   <c>true</c> if external account required; otherwise, <c>false</c>.
        /// </value>
        [JsonProperty("externalAccountRequired")]
        public bool ExternalAccountRequired { get; set; }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;

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
        [JsonProperty("terms-of-service")]
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
        [JsonProperty("caa-identities")]
        public IList<string> CaaIdentities { get; set; }
    }
}
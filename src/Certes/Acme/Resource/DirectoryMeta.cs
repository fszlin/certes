using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public Uri TermsOfService { get; }

        /// <summary>
        /// Gets or sets the website.
        /// </summary>
        /// <value>
        /// The website.
        /// </value>
        [JsonProperty("website")]
        public Uri Website { get; }

        /// <summary>
        /// Gets or sets the caa identities.
        /// </summary>
        /// <value>
        /// The caa identities.
        /// </value>
        [JsonProperty("caaIdentities")]
        public IList<string> CaaIdentities { get; }

        /// <summary>
        /// Gets or sets a value indicating whether [external account required].
        /// </summary>
        /// <value>
        ///   <c>true</c> if external account required; otherwise, <c>false</c>.
        /// </value>
        [JsonProperty("externalAccountRequired")]
        public bool? ExternalAccountRequired { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryMeta"/> class.
        /// </summary>
        /// <param name="termsOfService">The terms of service.</param>
        /// <param name="website">The website.</param>
        /// <param name="caaIdentities">The caa identities.</param>
        /// <param name="externalAccountRequired">The external account required.</param>
        public DirectoryMeta(
            Uri termsOfService,
            Uri website,
            IList<string> caaIdentities,
            bool? externalAccountRequired)
        {
            TermsOfService = termsOfService;
            Website = website;
            CaaIdentities = caaIdentities == null ? 
                (IList<string>)new string[0] :
                new ReadOnlyCollection<string>(caaIdentities);
            ExternalAccountRequired = externalAccountRequired;
        }
    }
}

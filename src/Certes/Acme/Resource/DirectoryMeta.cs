using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("termsOfService")]
        public Uri TermsOfService { get; }

        /// <summary>
        /// Gets or sets the website.
        /// </summary>
        /// <value>
        /// The website.
        /// </value>
        [JsonPropertyName("website")]
        public Uri Website { get; }

        /// <summary>
        /// Gets or sets the caa identities.
        /// </summary>
        /// <value>
        /// The caa identities.
        /// </value>
        [JsonPropertyName("caaIdentities")]
        public IList<string> CaaIdentities { get; }

        /// <summary>
        /// Gets or sets a value indicating whether [external account required].
        /// </summary>
        /// <value>
        ///   <c>true</c> if external account required; otherwise, <c>false</c>.
        /// </value>
        [JsonPropertyName("externalAccountRequired")]
        public bool? ExternalAccountRequired { get; }

        /// <summary>
        /// Gets the advertised certificate profile names and their human-readable descriptions.
        /// Descriptions may be prose or URLs. A null value means profiles were not advertised.
        /// </summary>
        [JsonPropertyName("profiles")]
        public IDictionary<string, string> Profiles { get; }

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
            : this(termsOfService, website, caaIdentities, externalAccountRequired, null)
        {
        }

        /// <summary>
        /// Initializes directory metadata including certificate profiles.
        /// </summary>
        /// <param name="termsOfService">The terms of service.</param>
        /// <param name="website">The website.</param>
        /// <param name="caaIdentities">The CAA identities.</param>
        /// <param name="externalAccountRequired">Whether external account binding is required.</param>
        /// <param name="profiles">The advertised profile names and descriptions.</param>
        [JsonConstructor]
        public DirectoryMeta(
            Uri termsOfService,
            Uri website,
            IList<string> caaIdentities,
            bool? externalAccountRequired,
            IDictionary<string, string> profiles)
        {
            TermsOfService = termsOfService;
            Website = website;
            CaaIdentities = caaIdentities == null ? 
                (IList<string>)new string[0] :
                new ReadOnlyCollection<string>(caaIdentities);
            ExternalAccountRequired = externalAccountRequired;
            Profiles = profiles == null ? null : new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>(profiles, StringComparer.Ordinal));
        }
    }
}

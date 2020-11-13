using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the ACME directory.
    /// </summary>
    public class AcmeDirectory
    {

        /// <summary>
        /// Gets or sets the new authorization endpoint.
        /// </summary>
        /// <value>
        /// The new authorization endpoint.
        /// </value>
        [JsonProperty("new-authz")]
        public Uri NewAuthz { get; set; }

        /// <summary>
        /// Gets or sets the revoke cert.
        /// </summary>
        /// <value>
        /// The revoke cert.
        /// </value>
        [JsonProperty("revoke-cert")]
        public Uri RevokeCert { get; set; }

        /// <summary>
        /// Gets or sets the key change endpoint.
        /// </summary>
        /// <value>
        /// The key change endpoint.
        /// </value>
        [JsonProperty("key-change")]
        public Uri KeyChange { get; set; }

        /// <summary>
        /// Gets or sets the new registration endpoint.
        /// </summary>
        /// <value>
        /// The new registration endpoint.
        /// </value>
        [JsonProperty("new-reg")]
        public Uri NewReg { get; set; }

        /// <summary>
        /// Gets or sets the new certificate endpoint.
        /// </summary>
        /// <value>
        /// The new certificate endpoint.
        /// </value>
        [JsonProperty("new-cert")]
        public Uri NewCert { get; set; }

        /// <summary>
        /// Gets or sets the metadata.
        /// </summary>
        /// <value>
        /// The metadata.
        /// </value>
        [JsonProperty("meta")]
        public AcmeDirectoryMeta Meta
        {
            get;
            set;
        }

        /// <summary>
        /// Represents the metadata for ACME directory.
        /// </summary>
        public class AcmeDirectoryMeta
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

            /// <summary>
            /// Indicate if ACME accounts requires external account binding (optional, https://tools.ietf.org/html/rfc8555#section-7.1.1)
            /// </summary>
            /// <value>
            /// If true, requires external account binding
            /// </value>
            [JsonProperty("externalAccountRequired")]
            public bool? ExternalAccountRequired { get; set; }
        }
    }
}

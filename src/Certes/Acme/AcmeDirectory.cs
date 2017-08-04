using Newtonsoft.Json;
using System;

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
        [Obsolete("Use Resource.DirectoryMeta instead.")]
        public AcmeDirectoryMeta Meta
        {
            get;
            set;
        }

        /// <summary>
        /// Represents the metadata for ACME directory.
        /// </summary>
        [Obsolete("Use Resource.DirectoryMeta instead.")]
        public class AcmeDirectoryMeta : Resource.DirectoryMeta
        {
        }
    }
}

using Newtonsoft.Json;
using System;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the ACME directory resource.
    /// </summary>
    public class Directory
    {
        /// <summary>
        /// Gets or sets the new nonce endpoint.
        /// </summary>
        /// <value>
        /// The new nonce endpoint.
        /// </value>
        [JsonProperty("new-nonce")]
        public Uri NewNonce { get; set; }

        /// <summary>
        /// Gets or sets the new account endpoint.
        /// </summary>
        /// <value>
        /// The new account endpoint.
        /// </value>
        [JsonProperty("new-account")]
        public Uri NewAccount { get; set; }

        /// <summary>
        /// Gets or sets the new order endpoint.
        /// </summary>
        /// <value>
        /// The new order endpoint.
        /// </value>
        [JsonProperty("new-order ")]
        public Uri NewOrder { get; set; }

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
        public DirectoryMeta Meta { get; set; }
    }
}
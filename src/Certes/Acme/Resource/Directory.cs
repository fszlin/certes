using System;
using Newtonsoft.Json;

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
        [JsonProperty("newNonce")]
        public Uri NewNonce { get; set; }

        /// <summary>
        /// Gets or sets the new account endpoint.
        /// </summary>
        /// <value>
        /// The new account endpoint.
        /// </value>
        [JsonProperty("newAccount")]
        public Uri NewAccount { get; set; }

        /// <summary>
        /// Gets or sets the new order endpoint.
        /// </summary>
        /// <value>
        /// The new order endpoint.
        /// </value>
        [JsonProperty("newOrder")]
        public Uri NewOrder { get; set; }

        /// <summary>
        /// Gets or sets the revoke cert.
        /// </summary>
        /// <value>
        /// The revoke cert.
        /// </value>
        [JsonProperty("revokeCert")]
        public Uri RevokeCert { get; set; }

        /// <summary>
        /// Gets or sets the key change endpoint.
        /// </summary>
        /// <value>
        /// The key change endpoint.
        /// </value>
        [JsonProperty("keyChange")]
        public Uri KeyChange { get; set; }

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

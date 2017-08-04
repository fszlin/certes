using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the ACME Order resource.
    /// </summary>
    /// <remarks>
    /// As https://tools.ietf.org/html/draft-ietf-acme-acme-07#section-7.1.3
    /// </remarks>
    public class Order
    {
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        /// <remarks>
        /// See <see cref="OrderStatus"/> for possible values.
        /// </remarks>
        [JsonProperty("status")]
        public OrderStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the expires.
        /// </summary>
        /// <value>
        /// The expires.
        /// </value>
        [JsonProperty("expires")]
        public DateTimeOffset Expires { get; set; }

        /// <summary>
        /// Gets or sets the CSR.
        /// </summary>
        /// <value>
        /// The CSR.
        /// </value>
        [JsonProperty("csr")]
        public string Csr { get; set; }

        /// <summary>
        /// Gets or sets the not before.
        /// </summary>
        /// <value>
        /// The not before.
        /// </value>
        [JsonProperty("notBefore")]
        public DateTimeOffset NotBefore { get; set; }

        /// <summary>
        /// Gets or sets the not after.
        /// </summary>
        /// <value>
        /// The not after.
        /// </value>
        [JsonProperty("notAfter")]
        public DateTimeOffset NotAfter { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        /// <remarks>
        /// TODO: model https://tools.ietf.org/html/rfc7807
        /// </remarks>
        [JsonProperty("error")]
        public object Error { get; set; }

        /// <summary>
        /// Gets or sets the authorizations.
        /// </summary>
        /// <value>
        /// The authorizations.
        /// </value>
        [JsonProperty("authorizations")]
        public IList<Uri> Authorizations { get; set; }

        /// <summary>
        /// Gets or sets the certificate.
        /// </summary>
        /// <value>
        /// The certificate.
        /// </value>
        [JsonProperty("certificate")]
        public Uri Certificate { get; set; }
    }
}

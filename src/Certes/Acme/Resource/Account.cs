using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the ACME Account resource.
    /// </summary>
    /// <remarks>
    /// As https://tools.ietf.org/html/draft-ietf-acme-acme-07#section-7.1.2
    /// </remarks>
    public class Account
    {
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <remarks>
        /// See <see cref="AccountStatus"/> for possible values.
        /// </remarks>
        /// <value>
        /// The status.
        /// </value>
        [JsonProperty("status")]
        public AccountStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the contact.
        /// </summary>
        /// <value>
        /// The contact.
        /// </value>
        [JsonProperty("contact")]
        public IList<string> Contact { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the terms of service is agreed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the terms of service is agreed; otherwise, <c>false</c>.
        /// </value>
        [JsonProperty("terms-of-service-agreed")]
        public bool TermsOfServiceAgreed { get; set; }

        /// <summary>
        /// Gets or sets the orders.
        /// </summary>
        /// <value>
        /// The orders.
        /// </value>
        [JsonProperty("orders")]
        public Uri Orders { get; set; }
    }
}

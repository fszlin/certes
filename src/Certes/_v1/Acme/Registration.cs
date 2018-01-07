using System;
using System.Collections.Generic;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the ACME Registration entity.
    /// </summary>
    /// <seealso cref="Certes.Acme.EntityBase" />
    public class Registration : EntityBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Registration"/> class.
        /// </summary>
        public Registration()
        {
            this.Resource = ResourceTypes.Registration;
        }

        /// <summary>
        /// Gets or sets the contact.
        /// </summary>
        /// <value>
        /// The contact.
        /// </value>
        public IList<string> Contact { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the accepted agreement URI.
        /// </summary>
        /// <value>
        /// The accepted agreement URI.
        /// </value>
        public Uri Agreement { get; set; }

        /// <summary>
        /// Gets or sets the authorizations.
        /// </summary>
        /// <value>
        /// The authorizations.
        /// </value>
        /// <remarks>Not being returned from Let's Encrypt servers.</remarks>
        public Uri Authorizations { get; set; }

        /// <summary>
        /// Gets or sets the certificates.
        /// </summary>
        /// <value>
        /// The certificates.
        /// </value>
        /// <remarks>Not being returned from Let's Encrypt servers.</remarks>
        public Uri Certificates { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether to delete the registration.
        /// </summary>
        /// <value>
        /// A flag indicating whether to delete the registration.
        /// </value>
        public bool? Delete { get; set; }
    }
}

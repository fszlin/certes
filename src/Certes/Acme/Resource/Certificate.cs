using System;
using System.Collections.Generic;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the ACME Certificate resource.
    /// </summary>
    public class Certificate
    {
        /// <summary>
        /// Gets or sets the identifiers.
        /// </summary>
        /// <value>
        /// The identifiers.
        /// </value>
        public IList<AuthorizationIdentifier> Identifiers { get; set; }

        /// <summary>
        /// Gets or sets the not before dates.
        /// </summary>
        /// <value>
        /// The not before dates.
        /// </value>
        public DateTimeOffset? NotBefore { get; set; }

        /// <summary>
        /// Gets or sets the not after dates.
        /// </summary>
        /// <value>
        /// The not after dates.
        /// </value>
        public DateTimeOffset? NotAfter { get; set; }
    }
}

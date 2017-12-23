using System;
using System.Collections.Generic;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the validation details for a challenge.
    /// </summary>
    public class ChallengeValidation
    {
        /// <summary>
        /// Gets or sets the URL used for validation.
        /// </summary>
        /// <value>
        /// The URL used for validation.
        /// </value>
        public Uri Url { get; set; }

        /// <summary>
        /// Gets or sets the host name used for validation.
        /// </summary>
        /// <value>
        /// The host name used for validation.
        /// </value>
        public string HostName { get; set; }

        /// <summary>
        /// Gets or sets the addresses resolved.
        /// </summary>
        /// <value>
        /// The addresses resolved.
        /// </value>
        public IList<string> AddressesResolved { get; set; }

        /// <summary>
        /// Gets or sets the address used.
        /// </summary>
        /// <value>
        /// The address used.
        /// </value>
        public string AddressUsed { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the ACME Challenge entity.
    /// </summary>
    /// <seealso cref="Certes.Acme.EntityBase" />
    public class Challenge : EntityBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Challenge"/> class.
        /// </summary>
        public Challenge()
        {
            this.Resource = ResourceTypes.Challenge;
        }
        /// <summary>
        /// Gets or sets the challenge type.
        /// </summary>
        /// <value>
        /// The challenge type.
        /// </value>
        /// <seealso cref="ChallengeTypes"/>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        /// <value>
        /// The URI.
        /// </value>
        public Uri Uri { get; set; }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        /// <seealso cref="EntityStatus"/>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the validated date.
        /// </summary>
        /// <value>
        /// The validated date.
        /// </value>
        public DateTimeOffset? Validated { get; set; }

        /// <summary>
        /// Gets or sets the key authorization string.
        /// </summary>
        /// <value>
        /// The key authorization string.
        /// </value>
        public string KeyAuthorization { get; set; }

        /// <summary>
        /// Gets or sets the validation records.
        /// </summary>
        /// <value>
        /// The validation records.
        /// </value>
        public IList<ChallengeValidation> ValidationRecord { get; set; }
    }

}

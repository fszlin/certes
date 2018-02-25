using System;
using System.Collections.Generic;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the ACME Authorization entity.
    /// </summary>
    /// <seealso cref="Certes.Acme.EntityBase" />
    public class AuthorizationEntity : EntityBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationEntity"/> class.
        /// </summary>
        public AuthorizationEntity()
        {
            this.Resource = ResourceTypes.Authorization;
        }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public AuthorizationIdentifier Identifier { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        /// <seealso cref="EntityStatus"/>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the expires date.
        /// </summary>
        /// <value>
        /// The expires date.
        /// </value>
        public DateTimeOffset Expires { get; set; }

        /// <summary>
        /// Gets or sets the challenges.
        /// </summary>
        /// <value>
        /// The challenges.
        /// </value>
        public IList<ChallengeEntity> Challenges { get; set; }

        /// <summary>
        /// Gets or sets the combinations.
        /// </summary>
        /// <value>
        /// The combinations.
        /// </value>
        public IList<IList<int>> Combinations { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace Certes.Acme
{
    /// <summary>
    /// Represents an ACME entity.
    /// </summary>
    public abstract class EntityBase
    {
        /// <summary>
        /// Gets or sets the resource type.
        /// </summary>
        /// <value>
        /// The resource type.
        /// </value>
        /// <seealso cref="ResourceTypes"/>
        public string Resource { get; set; }
    }

}

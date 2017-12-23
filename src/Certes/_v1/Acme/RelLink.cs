using System;

namespace Certes.Acme
{
    /// <summary>
    /// Represents a HTTP rel link.
    /// </summary>
    public class RelLink
    {
        /// <summary>
        /// Gets or sets the relation.
        /// </summary>
        /// <value>
        /// The relation.
        /// </value>
        public string Rel { get; set; }

        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        /// <value>
        /// The URI.
        /// </value>
        public Uri Uri { get; set; }
    }
}

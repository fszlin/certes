using System.Collections.Generic;
using System.Net;
using Certes.Acme.Resource;

namespace Certes.Acme
{
    /// <summary>
    /// Represents an error returned from ACME server.
    /// </summary>
    public class AcmeError
    {
        /// <summary>
        /// Gets or sets the error type URI.
        /// </summary>
        /// <value>
        /// The error type URI.
        /// </value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the detail message.
        /// </summary>
        /// <value>
        /// The detail message.
        /// </value>
        public string Detail { get; set; }

        /// <summary>
        /// Gets or sets the identifier for this error.
        /// </summary>
        /// <value>
        /// The identifier for this error.
        /// </value>
        public Identifier Identifier { get; set;}

        /// <summary>
        /// Gets or sets the subproblems.
        /// </summary>
        /// <value>
        /// The subproblems.
        /// </value>
        public IList<AcmeError> Subproblems { get; set; }

        /// <summary>
        /// Gets or sets the HTTP status.
        /// </summary>
        /// <value>
        /// The HTTP status.
        /// </value>
        public HttpStatusCode Status { get; set; }
    }
}

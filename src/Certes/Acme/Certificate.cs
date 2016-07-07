using System;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the ACME Certificate entity.
    /// </summary>
    /// <seealso cref="Certes.Acme.EntityBase" />
    public class Certificate : EntityBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Certificate"/> class.
        /// </summary>
        public Certificate()
        {
            Resource = ResourceTypes.Certificate;
        }

        /// <summary>
        /// Gets or sets the Certificate Signing Request (CSR).
        /// </summary>
        /// <value>
        /// The encoded Certificate Signing Request (CSR).
        /// </value>
        public string Csr { get; set; }

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

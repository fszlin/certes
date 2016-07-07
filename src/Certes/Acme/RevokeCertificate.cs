namespace Certes.Acme
{
    /// <summary>
    /// Represents the ACME revoke certificate entity.
    /// </summary>
    /// <seealso cref="Certes.Acme.EntityBase" />
    public class RevokeCertificate : EntityBase
    {
        /// <summary>
        /// Gets or sets the encoded certificate.
        /// </summary>
        /// <value>
        /// The encoded certificate.
        /// </value>
        public string Certificate { get; set; }
    }
}

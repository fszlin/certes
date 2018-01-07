namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the reasons for certificate revocation.
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc5280#section-5.3.1
    /// </remarks>
    //[JsonConverter(typeof(JsonConverter))]
    public enum RevocationReason
    {
        /// <summary>
        /// The unspecified reason.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Indicates the key is compromised.
        /// </summary>
        KeyCompromise = 1,

        /// <summary>
        /// Indicates the CAA is compromised.
        /// </summary>
        CACompromise = 2,

        /// <summary>
        /// Indicates the affiliation is changed.
        /// </summary>
        AffiliationChanged = 3,

        /// <summary>
        /// Indicates the certifcate is superseded.
        /// </summary>
        Superseded = 4,

        /// <summary>
        /// Indicates the Cessation of operation.
        /// </summary>
        CessationOfOperation = 5,

        /// <summary>
        /// Indicates the certificate is on hold.
        /// </summary>
        CertificateHold = 6,

        /// <summary>
        /// Indicates the certificate is removed from CRL.
        /// </summary>
        RemoveFromCRL = 8,

        /// <summary>
        /// Indicates privilege is withdrawn.
        /// </summary>
        PrivilegeWithdrawn = 9,

        /// <summary>
        /// Indicates AA is compromised.
        /// </summary>
        AACompromise = 10,
    }
}

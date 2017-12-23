namespace Certes.Acme
{
    /// <summary>
    /// The ACME resource types.
    /// </summary>
    public static class ResourceTypes
    {
        /// <summary>
        /// The new registration.
        /// </summary>
        public const string NewRegistration = "new-reg";

        /// <summary>
        /// The new authorization.
        /// </summary>
        public const string NewAuthorization = "new-authz";

        /// <summary>
        /// The new certificate.
        /// </summary>
        public const string NewCertificate = "new-cert";

        /// <summary>
        /// The revoke certificate.
        /// </summary>
        public const string RevokeCertificate = "revoke-cert";

        /// <summary>
        /// The registration.
        /// </summary>
        public const string Registration = "reg";

        /// <summary>
        /// The authorization.
        /// </summary>
        public const string Authorization = "authz";

        /// <summary>
        /// The challenge.
        /// </summary>
        public const string Challenge = "challenge";

        /// <summary>
        /// The certificate.
        /// </summary>
        public const string Certificate = "cert";

        /// <summary>
        /// The key change.
        /// </summary>
        public const string KeyChange = "key-change";
    }
}

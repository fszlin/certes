namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the status of <see cref="Account"/>.
    /// </summary>
    /// <remarks>
    /// As https://tools.ietf.org/html/draft-ietf-acme-acme-07#section-7.1.2
    /// </remarks>
    public static class AccountStatus
    {
        /// <summary>
        /// The valid status.
        /// </summary>
        public const string Valid = "valid";

        /// <summary>
        /// The deactivated status, initiated by client.
        /// </summary>
        public const string Deactivated = "deactivated";

        /// <summary>
        /// The revoked status, initiated by server.
        /// </summary>
        public const string Revoked = "revoked";
    }
}

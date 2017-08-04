namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents status of <see cref="AuthorizationIdentifier"/>.
    /// </summary>
    public static class AuthorizationIdentifierStatus
    {
        /// <summary>
        /// The pending status.
        /// </summary>
        public const string Pending = "pending";

        /// <summary>
        /// The processing status.
        /// </summary>
        public const string Processing = "processing";

        /// <summary>
        /// The valid status.
        /// </summary>
        public const string Valid = "valid";

        /// <summary>
        /// The invalid status.
        /// </summary>
        public const string Invalid = "invalid";

        /// <summary>
        /// The revoked status.
        /// </summary>
        public const string Revoked = "revoked";
    }
}

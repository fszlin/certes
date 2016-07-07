namespace Certes.Acme
{
    /// <summary>
    /// The ACME entity status.
    /// </summary>
    public static class EntityStatus
    {
        /// <summary>
        /// The pending status.
        /// </summary>
        public const string Pending = "pending";

        /// <summary>
        /// The unknown status.
        /// </summary>
        public const string Unknown = "unknown";

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

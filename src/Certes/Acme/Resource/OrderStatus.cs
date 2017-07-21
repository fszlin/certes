namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the status of <see cref="Order"/>.
    /// </summary>
    /// <remarks>
    /// As https://tools.ietf.org/html/draft-ietf-acme-acme-07#section-7.1.3
    /// </remarks>
    public class OrderStatus
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
    }

}

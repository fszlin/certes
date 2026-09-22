using System.Runtime.Serialization;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the status for <see cref="Challenge"/>.
    /// </summary>
    public enum ChallengeStatus
    {
        /// <summary>
        /// The pending status.
        /// </summary>
        [EnumMember(Value = "pending")]
        Pending,

        /// <summary>
        /// The processing status.
        /// </summary>
        [EnumMember(Value = "processing")]
        Processing,

        /// <summary>
        /// The valid status.
        /// </summary>
        [EnumMember(Value = "valid")]
        Valid,

        /// <summary>
        /// The invalid status.
        /// </summary>
        [EnumMember(Value = "invalid")]
        Invalid,
    }
}

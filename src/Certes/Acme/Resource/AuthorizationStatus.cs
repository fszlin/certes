using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the status of <see cref="Authorization"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AuthorizationStatus
    {
        /// <summary>
        /// The pending status.
        /// </summary>
        [EnumMember(Value = "pending")]
        Pending,

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

        /// <summary>
        /// The deactivated status.
        /// </summary>
        [EnumMember(Value = "deactivated")]
        Deactivated,

        /// <summary>
        /// The revoked status.
        /// </summary>
        [EnumMember(Value = "revoked")]
        Revoked,
    }
}

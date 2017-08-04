using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the status of <see cref="Order"/>.
    /// </summary>
    /// <remarks>
    /// As https://tools.ietf.org/html/draft-ietf-acme-acme-07#section-7.1.3
    /// </remarks>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OrderStatus
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

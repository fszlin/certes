using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the status for <see cref="Challenge"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ChallengeStatus
    {
        /// <summary>
        /// The pending status.
        /// </summary>
        [JsonProperty("pending")]
        Pending,

        /// <summary>
        /// The valid status.
        /// </summary>
        [JsonProperty("valid")]
        Valid,

        /// <summary>
        /// The invalid status.
        /// </summary>
        [JsonProperty("invalid")]
        Invalid,
    }
}

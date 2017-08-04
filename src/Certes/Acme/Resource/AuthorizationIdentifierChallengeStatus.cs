using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the status for <see cref="AuthorizationIdentifierChallenge"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AuthorizationIdentifierChallengeStatus
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

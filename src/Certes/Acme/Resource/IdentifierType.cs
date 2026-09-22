using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents type of <see cref="Identifier"/>.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum IdentifierType
    {
        /// <summary>
        /// The DNS type.
        /// </summary>
        [EnumMember(Value = "dns")]
        Dns,
    }
}

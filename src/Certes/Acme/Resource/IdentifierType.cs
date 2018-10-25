using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents type of <see cref="Identifier"/>.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IdentifierType
    {
        /// <summary>
        /// The DNS type.
        /// </summary>
        [EnumMember(Value = "dns")]
        Dns,
    }
}

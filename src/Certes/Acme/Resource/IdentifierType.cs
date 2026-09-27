using System.Runtime.Serialization;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents type of <see cref="Identifier"/>.
    /// </summary>
    public enum IdentifierType
    {
        /// <summary>
        /// The DNS type.
        /// </summary>
        [EnumMember(Value = "dns")]
        Dns,

        /// <summary>
        /// The IP address type, as defined in RFC 8738.
        /// </summary>
        [EnumMember(Value = "ip")]
        Ip,
    }
}

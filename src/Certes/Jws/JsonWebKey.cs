using Newtonsoft.Json;

namespace Certes.Jws
{
    /// <summary>
    /// Represents and JSON web key.
    /// </summary>
    public class JsonWebKey
    {
        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        /// <value>
        /// The type of the key.
        /// </value>
        [JsonProperty("kty")]
        internal string KeyType { get; set; }
    }
}

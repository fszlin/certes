using System.Text.Json.Serialization;

namespace Certes.Jws
{
    /// <summary>
    /// Represents and JSON web key. 
    /// Note that inheriting classes must define JSON serialisation order to maintain lexographic order as per https://tools.ietf.org/html/rfc7638#page-8
    /// </summary>
    public class JsonWebKey
    {
        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        /// <value>
        /// The type of the key.
        /// </value>
        [JsonPropertyName("kty")]
        [JsonPropertyOrder(2)]
        public string KeyType { get; set; }
    }
}

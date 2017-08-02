using Newtonsoft.Json;

namespace Certes.Jws
{
    /// <summary>
    /// Represents and JSON web key.
    /// </summary>
    public class JsonWebKey
    {
        /// <summary>
        /// Gets or sets the exponent.
        /// </summary>
        /// <value>
        /// The exponent.
        /// </value>
        [JsonProperty("e", Order = 0)]
        internal string Exponent { get; set; }

        /// <summary>
        /// Gets or sets the type of the key.
        /// </summary>
        /// <value>
        /// The type of the key.
        /// </value>
        [JsonProperty("kty", Order = 1)]
        internal string KeyType { get; set; }

        /// <summary>
        /// Gets or sets the modulus.
        /// </summary>
        /// <value>
        /// The modulus.
        /// </value>
        [JsonProperty("n", Order = 2)]
        internal string Modulus { get; set; }
    }
}

using Newtonsoft.Json;

namespace Certes.Jws
{
    /// <summary>
    /// Represents the unprotected header.
    /// </summary>
    public class JwsUnprotectedHeader
    {
        /// <summary>
        /// Gets or sets the algorithm.
        /// </summary>
        /// <value>
        /// The algorithm.
        /// </value>
        [JsonProperty("alg")]
        public string Algorithm { get; set; }

        /// <summary>
        /// Gets or sets the json web key.
        /// </summary>
        /// <value>
        /// The json web key.
        /// </value>
        [JsonProperty("jwk")]
        public JsonWebKey JsonWebKey { get; set; }

    }
}

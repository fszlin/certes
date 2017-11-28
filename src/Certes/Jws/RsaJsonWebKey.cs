using Newtonsoft.Json;

namespace Certes.Jws
{
    internal class RsaJsonWebKey : JsonWebKey
    {
        /// <summary>
        /// Gets or sets the exponent value for the RSA public key.
        /// </summary>
        /// <value>
        /// The exponent value for the RSA public key.
        /// </value>
        [JsonProperty("e")]
        internal string Exponent { get; set; }

        /// <summary>
        /// Gets or sets the modulus value for the RSA public key.
        /// </summary>
        /// <value>
        /// The modulus value for the RSA public key.
        /// </value>
        [JsonProperty("n")]
        internal string Modulus { get; set; }
    }
}

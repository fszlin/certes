﻿using Newtonsoft.Json;

namespace Certes.Jws
{
    /// <summary>
    /// Represents a JWK using RSA.
    /// </summary>
    /// <seealso cref="Certes.Jws.JsonWebKey" />
    internal class RsaJsonWebKey : JsonWebKey
    {
        /// <summary>
        /// Gets or sets the exponent value for the RSA public key.
        /// </summary>
        /// <value>
        /// The exponent value for the RSA public key.
        /// </value>
        [JsonProperty("e", Order = 1)]
        internal string Exponent { get; set; }

        /// <summary>
        /// Gets or sets the modulus value for the RSA public key.
        /// </summary>
        /// <value>
        /// The modulus value for the RSA public key.
        /// </value>
        [JsonProperty("n", Order =3)]
        internal string Modulus { get; set; }
    }
}

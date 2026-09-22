using System.Text.Json.Serialization;

namespace Certes.Jws
{
    /// <summary>
    /// Represents data signed with JWS.
    /// </summary>
    public class JwsPayload
    {
        /// <summary>
        /// Gets or sets the protected.
        /// </summary>
        /// <value>
        /// The protected.
        /// </value>
        [JsonPropertyName("protected")]
        public string Protected { get; set; }

        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        /// <value>
        /// The payload.
        /// </value>
        [JsonPropertyName("payload")]
        public string Payload { get; set; }

        /// <summary>
        /// Gets or sets the signature.
        /// </summary>
        /// <value>
        /// The signature.
        /// </value>
        [JsonPropertyName("signature")]
        public string Signature { get; set; }
    }

}

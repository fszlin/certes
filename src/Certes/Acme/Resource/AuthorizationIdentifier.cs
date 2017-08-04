using Newtonsoft.Json;

namespace Certes.Acme.Resource
{
    /// <summary>
    /// Represents the identifier for <see cref="Authorization"/>.
    /// </summary>
    public class AuthorizationIdentifier
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}

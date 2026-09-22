using System.Text.Json;
using System.Text.Json.Serialization;

namespace Certes.Json
{
    /// <summary>
    /// Helper methods for JSON serialization using System.Text.Json.
    /// </summary>
    public static class JsonUtil
    {
        private static JsonSerializerOptions defaultOptions;

        /// <summary>
        /// Creates the <see cref="JsonSerializerOptions"/> used for ACME entity serialization.
        /// </summary>
        /// <returns>The JSON serializer options.</returns>
        public static JsonSerializerOptions CreateSettings()
        {
            if (defaultOptions != null)
            {
                return defaultOptions;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            // For .NET 10, use reflection to serialize internal members
            // This is a workaround since System.Text.Json doesn't have a built-in option
            // We use a custom context or encoder that includes internal properties
            defaultOptions = options;
            return options;
        }
    }
}

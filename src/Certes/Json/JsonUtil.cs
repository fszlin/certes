using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Certes.Json
{
    /// <summary>
    /// Helper methods for JSON serialization.
    /// </summary>
    public static class JsonUtil
    {
        /// <summary>
        /// Creates the <see cref="JsonSerializerSettings"/> used for ACME entity serialization.
        /// </summary>
        /// <returns>The JSON serializer settings.</returns>
        public static JsonSerializerSettings CreateSettings()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            return jsonSettings;
        }
    }
}

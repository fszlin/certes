using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Certes.Jws;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
                ContractResolver = new ContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            return jsonSettings;
        }
    }

    internal sealed class ContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            if (typeof(JsonWebKey).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                return base.CreateProperties(type, memberSerialization).OrderBy(p => p.PropertyName).ToArray();
            }

            return base.CreateProperties(type, memberSerialization);
        }
    }
}

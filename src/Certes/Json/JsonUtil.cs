using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Certes.Jws;
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
                ContractResolver = new ContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            return jsonSettings;
        }
    }

    /// <summary>
    /// JSON contract resolver supports ordering JWK properties.
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver" />
    internal sealed class ContractResolver : CamelCasePropertyNamesContractResolver
    {
        /// <summary>
        /// Creates properties for the given <see cref="T:Newtonsoft.Json.Serialization.JsonContract" />.
        /// </summary>
        /// <param name="type">The type to create properties for.</param>
        /// <param name="memberSerialization">The member serialization mode for the type.</param>
        /// <returns>
        /// Properties for the given <see cref="T:Newtonsoft.Json.Serialization.JsonContract" />.
        /// </returns>
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

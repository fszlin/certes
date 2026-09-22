using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
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
                Converters = { new EnumMemberStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            defaultOptions = options;
            return options;
        }

        /// <summary>
        /// Custom enum converter that respects [EnumMember(Value = "...")] attributes.
        /// </summary>
        private class EnumMemberStringEnumConverter : JsonConverterFactory
        {
            private readonly JsonNamingPolicy namingPolicy;
            private readonly ConcurrentDictionary<Type, JsonConverter> converters = new();

            public EnumMemberStringEnumConverter(JsonNamingPolicy namingPolicy = null)
            {
                this.namingPolicy = namingPolicy;
            }

            public override bool CanConvert(Type typeToConvert)
            {
                return typeToConvert.IsEnum;
            }

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                return converters.GetOrAdd(typeToConvert, type =>
                    (JsonConverter)Activator.CreateInstance(
                        typeof(EnumMemberStringEnumValueConverter<>).MakeGenericType(type),
                        namingPolicy));
            }
        }

        /// <summary>
        /// Generic enum converter that respects [EnumMember] values.
        /// </summary>
        private class EnumMemberStringEnumValueConverter<T> : JsonConverter<T>
            where T : struct, Enum
        {
            private readonly JsonNamingPolicy namingPolicy;
            private readonly Dictionary<T, string> enumToString = new();
            private readonly Dictionary<string, T> stringToEnum = new(StringComparer.OrdinalIgnoreCase);

            public EnumMemberStringEnumValueConverter(JsonNamingPolicy namingPolicy)
            {
                this.namingPolicy = namingPolicy;

                foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (Enum.TryParse<T>(field.Name, out var enumValue))
                    {
                        var enumMember = field.GetCustomAttribute<EnumMemberAttribute>();
                        var stringValue = enumMember?.Value ?? namingPolicy?.ConvertName(field.Name) ?? field.Name;
                        
                        enumToString[enumValue] = stringValue;
                        stringToEnum[stringValue] = enumValue;
                    }
                }
            }

            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException($"Unexpected token {reader.TokenType} when parsing enum.");
                }

                var stringValue = reader.GetString();
                if (stringToEnum.TryGetValue(stringValue, out var value))
                {
                    return value;
                }

                throw new JsonException($"Value '{stringValue}' is not valid for enum type '{typeof(T).Name}'.");
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                if (enumToString.TryGetValue(value, out var stringValue))
                {
                    writer.WriteStringValue(stringValue);
                }
                else
                {
                    writer.WriteStringValue(value.ToString());
                }
            }
        }
    }
}

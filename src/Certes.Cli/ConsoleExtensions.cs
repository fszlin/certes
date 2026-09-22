using System.CommandLine;
using System.CommandLine.IO;
using System.Text.Json;
using Certes.Json;

namespace Certes.Cli
{
    internal static class ConsoleExtensions
    {
        private static readonly JsonSerializerOptions jsonSerializerSettings = JsonUtil.CreateSettings();

        public static void WriteAsJson(this IConsole console, object value)
        {
            console.Out.WriteLine(JsonSerializer.Serialize(value, jsonSerializerSettings));
        }
    }
}

using System.CommandLine;
using System.CommandLine.IO;
using System.Text.Json;

namespace Certes.Cli
{
    internal static class ConsoleExtensions
    {
        private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        public static void WriteAsJson(this IConsole console, object value)
        {
            console.Out.WriteLine(JsonSerializer.Serialize(value, jsonSerializerOptions));
        }
    }
}

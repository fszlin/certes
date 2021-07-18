using System.CommandLine;
using System.CommandLine.IO;
using Certes.Json;
using Newtonsoft.Json;

namespace Certes.Cli
{
    internal static class ConsoleExtensions
    {
        private static readonly JsonSerializerSettings jsonSerializerSettings = JsonUtil.CreateSettings();

        public static void WriteAsJson(this IConsole console, object value)
        {
            console.Out.WriteLine(JsonConvert.SerializeObject(value, jsonSerializerSettings));
        }
    }
}

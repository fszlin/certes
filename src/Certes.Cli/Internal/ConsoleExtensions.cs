using System;

namespace Certes.Cli.Internal
{
    internal static class ConsoleExtensions
    {
        public static void LogError(
            this IConsole console, Exception exception, string message, params object[] args) =>
            console.WriteLine(message, null, args);

        public static void LogWarning(
            this IConsole console, string message, params object[] args) =>
            console.WriteLine(message, null, args);

        public static void LogInformation(
            this IConsole console, string message, params object[] args) =>
            console.WriteLine(message, null, args);
    }
}

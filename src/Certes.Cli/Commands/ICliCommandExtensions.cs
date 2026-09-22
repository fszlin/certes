using System.CommandLine;

namespace Certes.Cli.Commands
{
    internal static class ICliCommandExtensions
    {
        public static string GetCommandName(this ICliCommand command)
            => command.Define().Name;

        public static string GetCommandDescription(this ICliCommand command)
            => command.Define().Description ?? string.Empty;
    }
}

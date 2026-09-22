using System.CommandLine;

namespace Certes.Cli.Commands
{
    internal static class ICliCommandExtensions
    {
        /// <summary>
        /// Gets the command name from the defined System.CommandLine.Command.
        /// </summary>
        public static string GetCommandName(this ICliCommand command)
        {
            return command.Define().Name;
        }

        /// <summary>
        /// Gets the command description from the defined System.CommandLine.Command.
        /// </summary>
        public static string GetCommandDescription(this ICliCommand command)
        {
            return command.Define().Description ?? "";
        }
    }
}

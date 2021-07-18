using System.CommandLine;

namespace Certes.Cli.Commands
{
    internal interface ICliCommand
    {
        CommandGroup Group { get; }
        Command Define();
    }
}

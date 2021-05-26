using System.CommandLine;

namespace Certes.Cli.Commands
{
    internal interface ICliCommandFactory
    {
        Command Create();
    }
}

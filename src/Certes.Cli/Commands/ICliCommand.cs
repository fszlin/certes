using System.CommandLine;
using System.Threading.Tasks;

namespace Certes.Cli.Commands
{
    internal interface ICliCommand
    {
        CommandGroup Group { get; }
        ArgumentCommand<string> Define(ArgumentSyntax syntax);
        Task<object> Execute(ArgumentSyntax syntax);
    }
}

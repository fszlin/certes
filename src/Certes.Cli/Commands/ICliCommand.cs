using System.CommandLine;
using System.Threading.Tasks;

namespace Certes.Cli.Commands
{
    internal interface ICliCommand
    {
        bool Define(ArgumentSyntax syntax);
        Task<object> Execute();
    }
}

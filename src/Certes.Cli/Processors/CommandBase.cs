using System.Threading.Tasks;
using Certes.Cli.Internal;
using Certes.Cli.Options;

namespace Certes.Cli.Processors
{
    internal abstract class CommandBase<T>
        where T : OptionsBase
    {
        public T Options { get; }
        public IConsole ConsoleLogger { get; }

        public CommandBase(T options, IConsole consoleLogger)
        {
            Options = options;
            ConsoleLogger = consoleLogger;
        }

        public abstract Task<AcmeContext> Process(AcmeContext context);
    }
}

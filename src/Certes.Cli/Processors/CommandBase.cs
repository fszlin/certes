using System.Threading.Tasks;
using Certes.Cli.Options;
using Microsoft.Extensions.Logging;

namespace Certes.Cli.Processors
{
    internal abstract class CommandBase<T>
        where T : OptionsBase
    {
        public T Options { get; }
        public ILogger ConsoleLogger { get; }

        public CommandBase(T options, ILogger consoleLogger)
        {
            Options = options;
            ConsoleLogger = consoleLogger;
        }

        public abstract Task<AcmeContext> Process(AcmeContext context);
    }
}

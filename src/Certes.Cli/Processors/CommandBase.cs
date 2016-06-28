using Certes.Cli.Options;
using System.Threading.Tasks;

namespace Certes.Cli.Processors
{
    internal abstract class CommandBase<T>
        where T : OptionsBase
    {
        public T Options { get; }

        public CommandBase(T options)
        {
            this.Options = options;
        }

        public abstract Task<AcmeContext> Process(AcmeContext context);
    }
}

using Autofac;
using Xunit;

namespace Certes.Cli
{
    public class ProgramTests
    {
        [Fact]
        public void CanResolveCli()
        {
            var container = Program.ConfigureContainer();
            var cli = container.Resolve<CliCoreSpectre>();
            Assert.NotNull(cli);
        }
    }
}

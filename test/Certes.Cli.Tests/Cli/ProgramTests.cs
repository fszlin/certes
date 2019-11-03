using System.Collections.Generic;
using Autofac;
using Certes.Cli.Commands;
using Xunit;

namespace Certes.Cli
{
    public class ProgramTests
    {
        [Fact]
        public void CanResolveCommands()
        {
            var container = Program.ConfigureContainer();
            var commands = container.Resolve<IEnumerable<ICliCommand>>();

            Assert.Contains(commands, c => c is ServerShowCommand);
            Assert.Contains(commands, c => c is ServerSetCommand);
            Assert.Contains(commands, c => c is AccountNewCommand);
        }

        [Fact]
        public void CanResolveCli()
        {
            var container = Program.ConfigureContainer();
            var cli = container.Resolve<CliCore>();
            Assert.NotNull(cli);
        }
    }
}

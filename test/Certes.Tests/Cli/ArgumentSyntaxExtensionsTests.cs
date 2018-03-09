using System.CommandLine;
using Xunit;

namespace Certes.Cli
{
    public class ArgumentSyntaxExtensionsTests
    {
        [Fact]
        public void CanGetOption()
        {
            var args = new[] { "--my-opt", "1" };
            var s = ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineOption("my-opt", "0");
            });

            Assert.Equal("1", s.GetOption<string>("my-opt", true));
            Assert.Throws<ArgumentSyntaxException>(() => s.GetOption<string>("another-opt", true));
        }
    }
}

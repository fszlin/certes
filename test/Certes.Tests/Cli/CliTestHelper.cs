using System.CommandLine;
using System.Linq;
using Xunit;

namespace Certes.Cli
{
    public static class CliTestHelper
    {
        public static void ValidateOption<T>(ArgumentSyntax syntax, string name, T value)
        {
            var arg = syntax.GetActiveOptions()
                .Where(p => p.Name == name)
                .OfType<Argument<T>>()
                .FirstOrDefault();
            Assert.NotNull(arg);
            Assert.Equal(value, arg.Value);
        }
    }
}

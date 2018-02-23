using System;
using System.CommandLine;
using System.Linq;
using Moq;
using Xunit;

namespace Certes.Cli
{
    internal static class CliTestHelper
    {
        public static void ValidateParameter<T>(ArgumentSyntax syntax, string name, T value)
        {
            var arg = syntax.GetActiveArguments()
                .Where(p => p.Name == name)
                .OfType<Argument<T>>()
                .FirstOrDefault();
            Assert.NotNull(arg);
            Assert.Equal(value, arg.Value);
        }

        public static void ValidateOption<T>(ArgumentSyntax syntax, string name, T value)
        {
            var arg = syntax.GetActiveOptions()
                .Where(p => p.Name == name)
                .OfType<Argument<T>>()
                .FirstOrDefault();
            Assert.NotNull(arg);
            Assert.Equal(value, arg.Value);
        }

        public static IAcmeContextFactory MakeFactory(Mock<IAcmeContext> ctxMock)
        {
            if (ctxMock == null)
            {
                ctxMock = new Mock<IAcmeContext>(MockBehavior.Strict);
            }

            var mock = new Mock<IAcmeContextFactory>(MockBehavior.Strict);
            mock.Setup(m => m.Create(It.IsAny<Uri>(), It.IsAny<IKey>())).Returns(ctxMock.Object);
            return mock.Object;
        }
    }
}

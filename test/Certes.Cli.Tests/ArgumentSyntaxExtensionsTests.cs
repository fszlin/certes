using System;
using System.CommandLine;
using System.Threading.Tasks;
using Certes.Cli.Settings;
using Moq;
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

        [Fact]
        public async Task CanReadKeyFromEnv()
        {
            var args = new[] { "--opt", "1" };
            var s = ArgumentSyntax.Parse(args, syntax =>
            {
                syntax.HandleErrors = false;
                syntax.DefineOption("opt", "0");
            });

            var envMock = new Mock<IEnvironmentVariables>(MockBehavior.Strict);
            var fileMock = new Mock<IFileUtil>(MockBehavior.Strict);

            envMock.Setup(m => m.GetVar(It.IsAny<string>())).Returns((string)null);
            await Assert.ThrowsAsync<CertesCliException>(
                () => s.ReadKey("key", "KEY", fileMock.Object, envMock.Object, true));

            envMock.Setup(m => m.GetVar("KEY")).Returns(Convert.ToBase64String(Helper.GetKeyV2().ToDer()));
            var key = await s.ReadKey("key", "KEY", fileMock.Object, envMock.Object);
            Assert.Equal(key.ToDer(), Helper.GetKeyV2().ToDer());
        }
    }
}

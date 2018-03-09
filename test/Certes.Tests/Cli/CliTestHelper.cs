using System;
using System.CommandLine;
using System.Linq;
using Certes.Cli.Settings;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Moq;
using Xunit;

namespace Certes.Cli
{
    internal static class CliTestHelper
    {
        public static void ValidateParameter<T>(ArgumentSyntax syntax, string name, T value)
        {
            var arg = syntax.GetActiveArguments()
                .Where(p => p.Names.Any(n => n == name))
                .OfType<Argument<T>>()
                .FirstOrDefault();
            Assert.NotNull(arg);
            Assert.Equal(value, arg.Value);
        }

        public static void ValidateOption<T>(ArgumentSyntax syntax, string name, T value)
        {
            var arg = syntax.GetActiveOptions()
                .Where(p => p.Names.Any(n => n == name))
                .OfType<Argument<T>>()
                .FirstOrDefault();
            Assert.NotNull(arg);
            Assert.Equal(value, arg.Value);
        }

        public static IResourceClientFactory MakeFactory(Mock<IResourceManagementClient> clientMock)
        {
            var mock = new Mock<IResourceClientFactory>(MockBehavior.Strict);
            mock.Setup(m => m.Create(It.IsAny<AzureCredentials>())).Returns(clientMock.Object);
            return mock.Object;
        }

        public static IAppServiceClientFactory MakeFactory(Mock<IWebSiteManagementClient> clientMock)
        {
            var mock = new Mock<IAppServiceClientFactory>(MockBehavior.Strict);
            mock.Setup(m => m.Create(It.IsAny<AzureCredentials>())).Returns(clientMock.Object);
            return mock.Object;
        }

        public static IDnsClientFactory MakeFactory(Mock<IDnsManagementClient> clientMock)
        {
            var mock = new Mock<IDnsClientFactory>(MockBehavior.Strict);
            mock.Setup(m => m.Create(It.IsAny<AzureCredentials>())).Returns(clientMock.Object);
            return mock.Object;
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

        public static IUserSettings NoopSettings()
        {
            var mock = new Mock<IUserSettings>();
            return mock.Object;
        }
    }
}

using System;
using System.CommandLine;
using System.Linq;
using System.Net.Http;
using Certes.Cli.Commands;
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

        public static IUserSettings NoopSettings()
        {
            var mock = new Mock<IUserSettings>();
            return mock.Object;
        }
    }
}

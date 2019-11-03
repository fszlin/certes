using System;
using Xunit;

namespace Certes.Cli.Settings
{
    public class EnvironmentVariablesTests
    {
        [Fact]
        public void CanGetEnvVar()
        {
            var env = new EnvironmentVariables();
            Assert.Equal(env.GetVar("PATH"), Environment.GetEnvironmentVariable("PATH"));
            Assert.Null(env.GetVar("CERTES_VAR_NOT_EXISTS"));
        }
    }
}

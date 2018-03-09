using System;

namespace Certes.Cli.Settings
{
    internal class EnvironmentVariables : IEnvironmentVariables
    {
        public string GetVar(string name)
            => Environment.GetEnvironmentVariable(name);
    }
}

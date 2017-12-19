using System;

namespace Certes.Cli.Internal
{
    internal interface IConsole
    {
        void WriteLine(string message, Exception exception = null, params object[] args);
    }
}

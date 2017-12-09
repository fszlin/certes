#if NETCOREAPP1_0 || NETCOREAPP2_0

using System;
using System.Collections.Generic;
using Certes.Cli.Internal;

namespace Certes.Cli
{
    internal class TestConsoleLogger : IConsole
    {
        public IList<string> Logs { get; } = new List<string>();

        public void WriteLine(string message, Exception exception = null, params object[] args)
        {
            Logs.Add(message);

            if (exception != null)
            {
                Logs.Add(exception.ToString());
            }
        }
    }
}

#endif

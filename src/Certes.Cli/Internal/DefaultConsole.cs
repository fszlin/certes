using System;

namespace Certes.Cli.Internal
{
    internal class DefaultConsole : IConsole
    {
        public void WriteLine(string message, Exception exception, params object[] args)
        {
            Console.WriteLine(message, args);

            if (exception != null)
            {
                Console.WriteLine(exception.ToString());
            }
        }
    }
}

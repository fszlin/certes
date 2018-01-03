#if NETCOREAPP1_0

using System;
using Microsoft.Extensions.Logging;

namespace Certes.Cli
{
    internal static class LoggerExtensions
    {
        public static void LogError(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.LogError(new EventId(), exception, message, args);
        }
    }
}

#endif

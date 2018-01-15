using System;

namespace Certes
{
    internal static class ContextFactory
    {
        public static Func<Uri, IKey, IAcmeContext> Create { get; set; } =
            (directoryUri, accountKey) => new AcmeContext(directoryUri, accountKey);
    }
}

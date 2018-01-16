using System;

namespace Certes.Cli
{
    internal static class ContextFactory
    {
        public static Func<Uri, IKey, IAcmeContext> DefaultCreate { get; } =
            (directoryUri, accountKey) => new Certes.AcmeContext(directoryUri, accountKey);

        public static Func<Uri, IKey, IAcmeContext> Create { get; set; } = DefaultCreate;
    }
}

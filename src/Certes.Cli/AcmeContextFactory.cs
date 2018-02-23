using System;

namespace Certes.Cli
{
    internal class AcmeContextFactory : IAcmeContextFactory
    {
        public IAcmeContext Create(Uri directoryUri, IKey accountKey)
            => new AcmeContext(directoryUri, accountKey);
    }
}

using System;

namespace Certes.Cli
{
    internal interface IAcmeContextFactory
    {
        IAcmeContext Create(Uri directoryUri, IKey accountKey);
    }
}

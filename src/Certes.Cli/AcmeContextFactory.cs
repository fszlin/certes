using System;

namespace Certes.Cli
{
    internal delegate IAcmeContext AcmeContextFactory(Uri directoryUri, IKey accountKey);
}

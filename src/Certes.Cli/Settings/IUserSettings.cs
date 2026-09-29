using System;
using System.Threading.Tasks;
using System.Threading;

namespace Certes.Cli.Settings
{
    internal interface IUserSettings
    {
        Task SetDefaultServer(Uri serverUri, CancellationToken cancellationToken = default);
        Task<Uri> GetDefaultServer(CancellationToken cancellationToken = default);
        Task<IKey> GetAccountKey(Uri serverUri, CancellationToken cancellationToken = default);
        Task SetAccountKey(Uri server, IKey key, CancellationToken cancellationToken = default);
    }
}

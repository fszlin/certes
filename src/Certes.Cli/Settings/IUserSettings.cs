using System;
using System.Threading.Tasks;

namespace Certes.Cli.Settings
{
    internal interface IUserSettings
    {
        Task SetDefaultServer(Uri serverUri);
        Task<Uri> GetDefaultServer();
        Task<IKey> GetAccountKey(Uri serverUri);
        Task SetAccountKey(Uri server, IKey key);
        Task<AzureSettings> GetAzureSettings();
    }
}

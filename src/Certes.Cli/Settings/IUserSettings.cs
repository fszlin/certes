using System;
using System.Threading.Tasks;

namespace Certes.Cli.Settings
{
    internal interface IUserSettings
    {
        Task SetDefaultServer(Uri serverUri);
        Task<Uri> GetDefaultServer();
    }
}

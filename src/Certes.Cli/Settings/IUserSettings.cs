using System;
using System.Threading.Tasks;

namespace Certes.Cli.Settings
{
    internal interface IUserSettings
    {
        Task SetServer(Uri serverUri);
    }

}

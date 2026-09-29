using System.Threading.Tasks;
using System.Threading;

namespace Certes.Cli
{
    internal interface IFileUtil
    {
        Task<string> ReadAllText(string path, CancellationToken cancellationToken = default);
        Task WriteAllText(string path, string text, CancellationToken cancellationToken = default);
        Task WriteAllBytes(string path, byte[] data, CancellationToken cancellationToken = default);
    }
}

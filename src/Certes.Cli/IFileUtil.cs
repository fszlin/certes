using System.Threading.Tasks;

namespace Certes.Cli
{
    internal interface IFileUtil
    {
        Task<string> ReadAllText(string path);
        Task WriteAllText(string path, string texts);
        Task WriteAllBytes(string path, byte[] data);
    }
}

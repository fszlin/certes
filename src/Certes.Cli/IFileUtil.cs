using System.Threading.Tasks;

namespace Certes.Cli
{
    internal interface IFileUtil
    {
        Task<string> ReadAllTexts(string path);
        Task WriteAllTexts(string path, string texts);
        Task WriteAllBytes(string path, byte[] data);
    }
}

using System.IO;
using System.Threading.Tasks;

namespace Certes.Cli
{
    public static class FileUtil
    {
        internal static async Task<string> ReadAllText(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }
    }
}

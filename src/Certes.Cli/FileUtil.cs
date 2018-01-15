using System.IO;
using System.Text;
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

        internal static Task WriteAllTexts(string path, string texts)
            => WriteAllBytes(path, Encoding.UTF8.GetBytes(texts));

        internal static async Task WriteAllBytes(string path, byte[] data)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }

            using (var stream = File.Create(path))
            {
                await stream.WriteAsync(data, 0, data.Length);
            }
        }
    }
}

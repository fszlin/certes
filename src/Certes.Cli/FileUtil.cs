using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Certes.Cli
{
    internal class FileUtil : IFileUtil
    {
        public async Task<string> ReadAllText(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using (var stream = File.OpenRead(path))
            {
                using (var reader = new StreamReader(stream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        public Task WriteAllText(string path, string text)
            => WriteAllBytes(path, Encoding.UTF8.GetBytes(text));

        public async Task WriteAllBytes(string path, byte[] data)
        {
            var fullPath = Path.GetFullPath(path);
            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using (var stream = File.Create(fullPath))
            {
                await stream.WriteAsync(data, 0, data.Length);
            }
        }
    }
}

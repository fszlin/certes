using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Certes.Cli
{
    public static class FileUtil
    {
        private static readonly IFileUtil file = new FileUtilImpl();

        internal static Task<string> ReadAllText(string path)
            => file.ReadAllTexts(path);

        internal static Task WriteAllTexts(string path, string texts)
            => file.WriteAllTexts(path, texts);
    }
}

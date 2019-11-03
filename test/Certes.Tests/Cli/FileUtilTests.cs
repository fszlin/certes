using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Certes.Cli
{
    public class FileUtilTests
    {
        [Fact]
        public async Task CanReadWriteText()
        {
            var file = new FileUtil();
            await file.WriteAllText("./Data/my-text.txt", "certes");
            Assert.Equal("certes", await file.ReadAllText("./Data/my-text.txt"));
            File.Delete("./Data/my-text.txt");

            await file.WriteAllText("./Data/new-dir/my-text.txt", "certes");
            Assert.Equal("certes", await file.ReadAllText("./Data/new-dir/my-text.txt"));
            Directory.Delete("./Data/new-dir/", true);
        }
        [Fact]
        public async Task NullIfNotExists()
        {
            var file = new FileUtil();
            Assert.Null(await file.ReadAllText("./Data/not-exists.txt"));
        }

        [Fact]
        public async Task CanWriteBytes()
        {
            var file = new FileUtil();
            await file.WriteAllBytes("./Data/my-text.txt", Encoding.UTF8.GetBytes("certes"));
            Assert.Equal("certes", await file.ReadAllText("./Data/my-text.txt"));
            File.Delete("./Data/my-text.txt");

            await file.WriteAllBytes("./Data/new-dir/my-text.txt", Encoding.UTF8.GetBytes("certes"));
            Assert.Equal("certes", await file.ReadAllText("./Data/new-dir/my-text.txt"));
            Directory.Delete("./Data/new-dir/", true);
        }
    }
}

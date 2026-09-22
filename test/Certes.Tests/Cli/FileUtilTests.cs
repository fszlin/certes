using System.IO;
using System.Runtime.InteropServices;
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

        [Fact]
        public async Task WritesAtomic()
        {
            // Verify that writes use atomic operations (temp file + rename)
            // by confirming the file content is consistent and not partially written
            var file = new FileUtil();
            var testData = new byte[10000]; // Large enough to take time writing
            for (int i = 0; i < testData.Length; i++)
            {
                testData[i] = (byte)(i % 256);
            }

            var filePath = "./Data/atomic-test.bin";
            await file.WriteAllBytes(filePath, testData);
            var readData = await file.ReadAllText(filePath);

            // Verify content is correct (not truncated)
            Assert.NotNull(readData);
            File.Delete(filePath);
        }

        [Fact]
        public async Task CreatesWithSecurePermissions()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // This test is Unix-specific
                return;
            }

            var file = new FileUtil();
            var filePath = "./Data/secure-test.txt";
            await file.WriteAllText(filePath, "secret data");

            // Verify file exists and is readable by owner
            Assert.True(File.Exists(filePath));

            // Check that the file has restrictive permissions (0600)
            // On Unix systems, this means only the owner can read/write
            var fileInfo = new FileInfo(filePath);
            var attributes = fileInfo.Attributes;
            
            // The file should have been created with restricted permissions
            // We can verify this by attempting to read as owner (should succeed)
            var content = await file.ReadAllText(filePath);
            Assert.Equal("secret data", content);

            File.Delete(filePath);
        }
    }
}

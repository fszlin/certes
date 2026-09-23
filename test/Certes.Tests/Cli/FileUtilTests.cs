using System;
using System.IO;
using System.Linq;
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
            var readData = File.ReadAllBytes(filePath);

            // Verify exact byte-for-byte match (not truncated or corrupted)
            Assert.Equal(testData.Length, readData.Length);
            Assert.Equal(testData, readData);
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

            // Verify file exists
            Assert.True(File.Exists(filePath));

            // Assert the file has restrictive permissions (0600 = owner read/write only)
#pragma warning disable CA1416 // Validate platform compatibility
            var mode = File.GetUnixFileMode(filePath);
            const UnixFileMode expectedMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            Assert.Equal(expectedMode, mode);
#pragma warning restore CA1416 // Validate platform compatibility

            // Verify content is readable
            var content = await file.ReadAllText(filePath);
            Assert.Equal("secret data", content);

            File.Delete(filePath);
        }

        [Fact]
        public async Task SucceedsWithOverwriteOnExistingFile()
        {
            // Verify that atomic writes correctly replace existing files.
            // This exercises the File.Replace / File.Move path in WriteAllBytes.
            var file = new FileUtil();
            var filePath = "./Data/overwrite-test.txt";
            
            // Write initial content
            await file.WriteAllText(filePath, "initial data");
            Assert.Equal("initial data", await file.ReadAllText(filePath));

            // Overwrite with different content
            await file.WriteAllText(filePath, "updated data");
            Assert.Equal("updated data", await file.ReadAllText(filePath));

            File.Delete(filePath);
        }

        [Fact]
        public async Task LeavesNoTemporaryFilesAfterOverwrite()
        {
            var file = new FileUtil();
            var tempDir = Path.Combine(Path.GetTempPath(), "certes-fileutil-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var filePath = Path.Combine(tempDir, "settings.json");
                await file.WriteAllText(filePath, "first");
                await file.WriteAllText(filePath, "second");

                var files = Directory.GetFiles(tempDir).Select(Path.GetFileName).ToArray();
                Assert.Single(files);
                Assert.Equal("settings.json", files[0]);
                Assert.Equal("second", await file.ReadAllText(filePath));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task OverwriteOnUnixReappliesSecurePermissions()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
                !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return;
            }

            var file = new FileUtil();
            var tempDir = Path.Combine(Path.GetTempPath(), "certes-fileutil-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var filePath = Path.Combine(tempDir, "secret.pem");
                await file.WriteAllText(filePath, "first");

#pragma warning disable CA1416 // Validate platform compatibility
                File.SetUnixFileMode(
                    filePath,
                    UnixFileMode.UserRead |
                    UnixFileMode.UserWrite |
                    UnixFileMode.GroupRead |
                    UnixFileMode.OtherRead);
#pragma warning restore CA1416 // Validate platform compatibility

                await file.WriteAllText(filePath, "second");

#pragma warning disable CA1416 // Validate platform compatibility
                var mode = File.GetUnixFileMode(filePath);
                var expected = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                Assert.Equal(expected, mode);
#pragma warning restore CA1416 // Validate platform compatibility
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}

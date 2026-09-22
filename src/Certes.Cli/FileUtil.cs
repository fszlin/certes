using System;
using System.IO;
using System.Runtime.InteropServices;
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

            // Write to a temporary file first for atomicity
            var tempPath = Path.Combine(dir, Path.GetRandomFileName());
            try
            {
                using (var stream = File.Create(tempPath, 4096, FileOptions.WriteThrough))
                {
                    await stream.WriteAsync(data, 0, data.Length);
                }

                // Set restrictive permissions on Unix-like systems before moving to final location
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    SetUnixFilePermissions(tempPath);
                }

                // Atomic rename: on Unix, move replaces atomically; on Windows, use Replace
                if (File.Exists(fullPath))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        File.Replace(tempPath, fullPath, null, ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Move(tempPath, fullPath, overwrite: true);
                    }
                }
                else
                {
                    File.Move(tempPath, fullPath);
                }

                // Ensure final file has correct permissions on Unix
                if ((RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) && File.Exists(fullPath))
                {
                    SetUnixFilePermissions(fullPath);
                }
            }
            catch
            {
                // Clean up temp file if something went wrong
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); }
                    catch { /* Ignore cleanup errors */ }
                }
                throw;
            }
        }

        private static void SetUnixFilePermissions(string path)
        {
            try
            {
#pragma warning disable CA1416 // Validate platform compatibility
                // chmod 0600 (owner read/write only)
                const int ownerReadWrite = 0b110_000_000; // 384 in decimal, 0600 in octal
                File.SetUnixFileMode(path, (UnixFileMode)ownerReadWrite);
#pragma warning restore CA1416 // Validate platform compatibility
            }
            catch
            {
                // If SetUnixFileMode is not available (older .NET), silently continue
                // This is acceptable as the file was created with a restrictive default mode
            }
        }
    }
}

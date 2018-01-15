#if NETCOREAPP1_0 || NETCOREAPP2_0

using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Certes.Cli.Options
{
    public class OptionsExtensionsTests
    {
        [Fact]
        public async Task CanLoadKeyFromPath()
        {
            File.WriteAllText("./Data/key-es256.pem", Helper.GetTestKey(KeyAlgorithm.ES256));

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
                Path = "./Data/key-es256.pem",
            };

            var key = await options.LoadKey();

            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task CanLoadKeyFromUnixEvn()
        {
            if (!Directory.Exists("./.certes/"))
            {
                Directory.CreateDirectory("./.certes/");
            }

            File.WriteAllText("./.certes/account.pem", Helper.GetTestKey(KeyAlgorithm.ES256));

            var fullPath = Path.GetFullPath("./");
            Environment.SetEnvironmentVariable("HOMEDRIVE", "");
            Environment.SetEnvironmentVariable("HOMEPATH", "");
            Environment.SetEnvironmentVariable("HOME", fullPath);

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            var key = await options.LoadKey();

            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task CanLoadKeyFromWinEvn()
        {
            if (!Directory.Exists("./.certes/"))
            {
                Directory.CreateDirectory("./.certes/");
            }

            File.WriteAllText("./.certes/account.pem", Helper.GetTestKey(KeyAlgorithm.ES256));

            var fullPath = Path.GetFullPath("./");
            var drive = Path.GetPathRoot(fullPath);
            Environment.SetEnvironmentVariable("HOME", "");
            Environment.SetEnvironmentVariable("HOMEDRIVE", drive);
            Environment.SetEnvironmentVariable("HOMEPATH", fullPath.Substring(drive.Length));

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            var key = await options.LoadKey();

            Assert.Equal(Helper.GetKeyV2(KeyAlgorithm.ES256).Thumbprint(), key.Thumbprint());
        }

        [Fact]
        public async Task NullWhenKeyNotExist()
        {
            if (Directory.Exists("./.certes/"))
            {
                Directory.Delete("./.certes/", true);
            }

            var options = new AccountOptions
            {
                Action = AccountAction.Info,
            };

            var key = await options.LoadKey();
            Assert.Null(key);
        }
    }
}

#endif

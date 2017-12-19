#if NETCOREAPP1_0 || NETCOREAPP2_0

using System;
using System.IO;
using System.Threading.Tasks;
using Certes.Cli.Options;
using Certes.Jws;
using Certes.Pkcs;
using Xunit;

namespace Certes.Cli.Processors
{
    public class ImportCommandTests
    {
        [Fact]
        public async Task ProcessWithExistingContext()
        {
            var options = new ImportOptions
            {
                Force = false,
            };

            var cmd = new ImportCommand(options, new TestConsoleLogger());
            await Assert.ThrowsAsync<Exception>(() =>
                cmd.Process(new AcmeContext()));
        }

        [Fact]
        public async Task ProcessWithoutKeyFile()
        {
            var options = new ImportOptions
            {
                KeyFile = null,
            };

            var cmd = new ImportCommand(options, new TestConsoleLogger());
            await Assert.ThrowsAsync<Exception>(() =>
                cmd.Process(null));
        }

        [Fact]
        public async Task CanLoadKey()
        { 
            if (!Directory.Exists("./_test"))
            {
                Directory.CreateDirectory("./_test");
        
            }

            var options = new ImportOptions
            {
                KeyFile = "./_test/key.pem",
            };

            var key = new AccountKey(SignatureAlgorithm.ES256);
            using (var fs = File.Create(options.KeyFile))
            {
                key.Export().Save(fs);
            }

            var cmd = new ImportCommand(options, new TestConsoleLogger());
            var ctx = await cmd.Process(null);

            Assert.NotNull(ctx.Account);
            Assert.Equal(
                key.Export().PrivateKeyInfo,
                ctx.Account.Key.PrivateKeyInfo);
        }
    }
}

#endif

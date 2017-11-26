using System.IO;
using System.Threading.Tasks;
using Certes.Jws;
using Xunit;

namespace Certes.Pkcs
{
    public class PfxBuilderTests
    {
        [Fact]
        public async Task CanCreatePfxChain()
        {
            await Task.Yield();
            var leafCert = File.ReadAllBytes("./Data/leaf-cert.cer");

            var pfxBuilder = new PfxBuilder(leafCert, new AccountKey().Export());

            pfxBuilder.AddIssuer(File.ReadAllBytes("./Data/test-ca2.cer"));
            pfxBuilder.AddIssuer(File.ReadAllBytes("./Data/test-root.cer"));
            var pfx = pfxBuilder.Build("my-cert", "abcd1234");
        }
    }
}

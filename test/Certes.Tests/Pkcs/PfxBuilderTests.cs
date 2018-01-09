using System.IO;
using System.Threading.Tasks;
using Certes.Jws;
using Xunit;

namespace Certes.Pkcs
{
    public class PfxBuilderTests
    {
        [Theory]
        [InlineData(SignatureAlgorithm.RS256)]
        [InlineData(SignatureAlgorithm.ES256)]
        [InlineData(SignatureAlgorithm.ES384)]
        [InlineData(SignatureAlgorithm.ES512)]
        public async Task CanCreatePfxChain(SignatureAlgorithm alog)
        {
            await Task.Yield();
            var leafCert = File.ReadAllBytes("./Data/leaf-cert.cer");

            var pfxBuilder = new PfxBuilder(leafCert, new AccountKey(alog).Export());

            pfxBuilder.AddIssuer(File.ReadAllBytes("./Data/test-ca2.pem"));
            pfxBuilder.AddIssuer(File.ReadAllBytes("./Data/test-root.pem"));
            var pfx = pfxBuilder.Build("my-cert", "abcd1234");
        }
    }
}

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Certes.Pkcs
{
    [Collection(nameof(Helper.GetValidCert))]
    public class PfxBuilderTests
    {
        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        public async Task CanCreatePfxChain(KeyAlgorithm alog)
        {
            var cert = await Helper.GetValidCert();

            var pfxBuilder = new PfxBuilder(
                Encoding.UTF8.GetBytes(cert), KeyFactory.NewKey(alog));
            pfxBuilder.AddTestCerts();
            pfxBuilder.AddIssuers(Encoding.UTF8.GetBytes(cert));
            var pfx = pfxBuilder.Build("my-cert", "abcd1234");
        }

        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        public void CanCreatePfxWithoutChain(KeyAlgorithm alog)
        {
            var leafCert = File.ReadAllText("./Data/leaf-cert.pem");

            var pfxBuilder = new PfxBuilder(
                Encoding.UTF8.GetBytes(leafCert), KeyFactory.NewKey(alog));
            pfxBuilder.FullChain = false;
            var pfx = pfxBuilder.Build("my-cert", "abcd1234");
        }
    }
}

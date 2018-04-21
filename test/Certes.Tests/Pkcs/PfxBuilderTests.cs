using System.IO;
using System.Text;
using Org.BouncyCastle.Pkix;
using Xunit;

namespace Certes.Pkcs
{
    public class PfxBuilderTests
    {
        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        public void CanCreatePfxChain(KeyAlgorithm alog)
        {
            var leafCert = File.ReadAllText("./Data/leaf-cert.pem");

            var pfxBuilder = new PfxBuilder(
                Encoding.UTF8.GetBytes(leafCert), KeyFactory.NewKey(alog));

            pfxBuilder.AddIssuer(File.ReadAllBytes("./Data/test-ca2.pem"));
            pfxBuilder.AddIssuer(File.ReadAllBytes("./Data/test-root.pem"));
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

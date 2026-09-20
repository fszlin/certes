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
        public void CanCreatePfxChain(KeyAlgorithm algorithm)
        {
            var fixture = new CertificateFixture(algorithm);
            var pfxBuilder = fixture.Chain.ToPfx(fixture.Key);
            var pfx = pfxBuilder.Build("my-cert", "abcd1234");
            fixture.AssertPfx(pfx, "abcd1234", "my-cert");
        }

        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        public void CanCreatePfxWithoutChain(KeyAlgorithm algorithm)
        {
            var fixture = new CertificateFixture(algorithm);
            var pfxBuilder = new PfxBuilder(fixture.Leaf.GetEncoded(), fixture.Key);
            pfxBuilder.FullChain = false;
            var pfx = pfxBuilder.Build("my-cert", "abcd1234");
            fixture.AssertPfx(pfx, "abcd1234", "my-cert", fullChain: false);
        }

        [Fact]
        public void FullChainRequiresIssuers()
        {
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var builder = new PfxBuilder(fixture.Leaf.GetEncoded(), fixture.Key);
            Assert.Throws<AcmeException>(() => builder.Build("my-cert", "abcd1234"));
        }
    }
}

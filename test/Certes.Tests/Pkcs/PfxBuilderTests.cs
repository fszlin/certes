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

        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        public void CanCreatePfxChainWithoutRoot(KeyAlgorithm algorithm)
        {
            // RFC 8555, section 7.4.2: the server is not expected to supply the self-signed root.
            var fixture = new CertificateFixture(algorithm);
            var chain = CertificateFixture.ChainOf(fixture.Leaf, fixture.Intermediate);

            var pfx = chain.ToPfx(fixture.Key).Build("my-cert", "abcd1234");

            fixture.AssertPfx(pfx, "abcd1234", "my-cert");
            fixture.AssertPfxChain(pfx, "abcd1234", fixture.Leaf, fixture.Intermediate);
        }

        [Fact]
        public void PfxChainExcludesRootWhenSupplied()
        {
            // The root is used as a trust anchor only; it is not part of the exported chain.
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var pfx = fixture.Chain.ToPfx(fixture.Key).Build("my-cert", "abcd1234");

            fixture.AssertPfxChain(pfx, "abcd1234", fixture.Leaf, fixture.Intermediate);
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

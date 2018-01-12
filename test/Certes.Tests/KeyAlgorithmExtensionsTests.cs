using Xunit;

namespace Certes
{
    public class KeyAlgorithmExtensionsTests
    {
        [Fact]
        public void CanGetAlgoId()
        {
            Assert.Equal("1.2.840.10045.4.3.2", KeyAlgorithm.ES256.ToPkcsObjectId());
            Assert.Equal("1.2.840.10045.4.3.3", KeyAlgorithm.ES384.ToPkcsObjectId());
            Assert.Equal("1.2.840.10045.4.3.4", KeyAlgorithm.ES512.ToPkcsObjectId());
            Assert.Equal("1.2.840.113549.1.1.11", KeyAlgorithm.RS256.ToPkcsObjectId());
        }
    }
}

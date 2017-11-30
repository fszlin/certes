using System;
using Certes.Pkcs;
using Xunit;

namespace Certes.Crypto
{
    public class SignatureAlgorithmProviderTests
    {
        [Theory]
        [InlineData(SignatureAlgorithm.ES256)]
        [InlineData(SignatureAlgorithm.ES384)]
        [InlineData(SignatureAlgorithm.ES512)]
        private void CanCreateEllipticCurveAlgo(SignatureAlgorithm signatureAlgorithm)
        {
            var provider = new SignatureAlgorithmProvider();
            var algo = provider.Get(signatureAlgorithm) as EllipticCurveSignatureAlgorithm;

            Assert.NotNull(algo);
        }

        [Fact]
        public void CtorWithInvalidAlgo()
        {
            var provider = new SignatureAlgorithmProvider();
            Assert.Throws<ArgumentException>(() =>
                provider.Get((SignatureAlgorithm)100));
        }
    }
}

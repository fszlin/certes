using System;
using System.IO;
using System.Threading.Tasks;
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

        [Theory]
        [InlineData(SignatureAlgorithm.RS256)]
        [InlineData(SignatureAlgorithm.ES256)]
        [InlineData(SignatureAlgorithm.ES384)]
        [InlineData(SignatureAlgorithm.ES512)]
        public async Task CanGetKey(SignatureAlgorithm signatureAlgorithm)
        {
            var provider = new SignatureAlgorithmProvider();
            var algo = provider.Get(signatureAlgorithm);
            var key = (AsymmetricCipherSignatureKey)algo.GenerateKey();

            using (var data = new MemoryStream())
            {
                await key.Save(data);
                var keyData = data.ToArray();
                data.Seek(0, SeekOrigin.Begin);

                var restored = provider.GetKey(data);

                using (var buffer = new MemoryStream())
                {
                    await restored.Save(buffer);
                    var restoredData = buffer.ToArray();
                    Assert.Equal(keyData, restoredData);
                }
            }
        }
    }
}

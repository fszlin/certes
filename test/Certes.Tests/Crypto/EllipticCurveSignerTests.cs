using System;
using System.Security.Cryptography;
using System.Text;
using Certes.Pkcs;
using Xunit;

namespace Certes.Crypto
{
    public class EllipticCurveSignerTests
    {
        [Fact]
        public void InvalidPrivateKey()
        {
            var provider = new KeyAlgorithmProvider();
            var algo = provider.Get(KeyAlgorithm.RS256);
            var key = algo.GenerateKey();

            Assert.Throws<ArgumentException>(() => new EllipticCurveSigner(key, "algo", "algo"));
        }

        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        public void CanComputeHash(KeyAlgorithm algoType)
        {
            var provider = new KeyAlgorithmProvider();
            var algo = provider.Get(algoType);
            var signer = algo.CreateSigner(algo.GenerateKey());

            var data = Encoding.UTF8.GetBytes("secret message");
            var hash = signer.ComputeHash(data);

            using (var sha = SHA256.Create())
            {
                Assert.Equal(sha.ComputeHash(data), hash);
            }
        }
    }
}

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
            var provider = new SignatureAlgorithmProvider();
            var algo = provider.Get(SignatureAlgorithm.RS256);
            var key = algo.GenerateKey();

            Assert.Throws<ArgumentException>(() => new EllipticCurveSigner(key, "algo", "algo"));
        }

        [Theory]
        [InlineData(SignatureAlgorithm.RS256)]
        [InlineData(SignatureAlgorithm.ES256)]
        [InlineData(SignatureAlgorithm.ES384)]
        [InlineData(SignatureAlgorithm.ES512)]
        public void CanComputeHash(SignatureAlgorithm algoType)
        {
            var provider = new SignatureAlgorithmProvider();
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

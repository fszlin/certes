using System;
using System.Text;
using Xunit;

namespace Certes.Jws
{
    public class AccountKeyTests
    {
        [Fact]
        public void CreateWithNull()
        {
            Assert.Throws<ArgumentNullException>(() => new AccountKey(null));
        }

        [Fact]
        public void CanGenerateThumbprint()
        {
            var key = new AccountKey();
            var bytes = key.GenerateThumbprint();
            Assert.NotNull(bytes);
        }

        [Fact]
        public void CanGenerateKeyAuthorization()
        {
            var key = new AccountKey();
            var keyAuthz = key.KeyAuthorization("t0001");
            Assert.Equal("t0001." + key.Thumbprint(), keyAuthz);
        }

        [Fact]
        public void CanGetKeyAlgo()
        {
            var key = new AccountKey(KeyAlgorithm.ES512);
            Assert.Equal(KeyAlgorithm.ES512, key.Algorithm);
        }

        [Fact]
        public void CanComputeHash()
        {
            var key = new AccountKey(KeyAlgorithm.ES512);
            var hash = key.ComputeHash(Encoding.UTF8.GetBytes("TEST"));
            Assert.NotNull(hash);
        }
    }
}

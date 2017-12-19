using System;
using Certes.Pkcs;
using Xunit;

namespace Certes.Crypto
{
    public class AsymmetricCipherSignatureKeyTests
    {
        [Fact]
        public void CtorNull()
        {
            Assert.Throws<ArgumentNullException>(() => new AsymmetricCipherSignatureKey(SignatureAlgorithm.ES256, null));
        }
    }
}

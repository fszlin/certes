using System;
using Xunit;

namespace Certes.Pkcs
{
    public class KeyInfoTests
    {
        [Fact]
        public void CanReloadKeyPair()
        {
            var keyInfo = new KeyInfo
            {
                PrivateKeyInfo = Convert.FromBase64String(Helper.PrivateKey)
            };

            var keyPair = keyInfo.CreateKeyPair();
            var exported = keyPair.Export();

            Assert.Equal(Helper.PrivateKey, Convert.ToBase64String(exported.PrivateKeyInfo));
        }
    }
}

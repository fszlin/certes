using System;
using System.IO;
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
                PrivateKeyInfo = Convert.FromBase64String(Helper.GetTestKeyV1())
            };

            var keyPair = keyInfo.CreateKeyPair();
            var exported = keyPair.Export();

            Assert.Equal(Helper.GetTestKeyV1(), Convert.ToBase64String(exported.PrivateKeyInfo));
        }

        [Fact]
        public void LoadKeyWithInvalidObject()
        {
            Assert.Throws<AcmeException>(() => KeyInfo.From(new MemoryStream()));

        }
    }
}

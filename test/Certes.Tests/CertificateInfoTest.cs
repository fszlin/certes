using System;
using System.IO;
using Xunit;

namespace Certes
{
    public class CertificateInfoTest
    {
        [Fact]
        public void CanGeneratePfx()
        {
            var cert = File.ReadAllText("./Data/cert-es256.pem");
            var key = Helper.GetKeyV2(KeyAlgorithm.ES256);

            var data = new CertificateInfo(cert, key);
            var pfx = data.ToPfx("my-pfx", "abcd1234", false);
        }

        [Fact]
        public void PreventPfxWhenNoKey()
        {
            var cert = File.ReadAllText("./Data/cert-es256.pem");

            var data = new CertificateInfo(cert, null);
            Assert.Throws<InvalidOperationException>(() => data.ToPfx("my-pfx", "abcd1234", false));
        }
    }
}

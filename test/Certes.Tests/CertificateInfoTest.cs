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

            File.WriteAllBytes("./Data/cert-es256.pfx", pfx);
        }
    }
}

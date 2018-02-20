using Xunit;

namespace Certes.Acme
{
    public class RevokeCertificateTests
    {

        [Fact]
        public void CanGetSetProperties()
        {
            var authz = new RevokeCertificate();
            authz.VerifyGetterSetter(a => a.Certificate, "pem");
        }
    }
}

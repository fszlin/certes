using System;
using Xunit;

namespace Certes.Acme
{
    public class AcmeCertificateTests
    {
        // [Fact]
        public void ThrowExceptionWhenCertDataMissing()
        {
            var cert = new AcmeCertificate
            {
                Location = new Uri("http://my-cert-url.com", UriKind.Absolute),
                Raw = null
            };

            var ex = Assert.Throws<AcmeException>(() => AcmeCertificateExtensions.ToPfx(cert));
            Assert.Contains(cert.Location.AbsoluteUri, ex.Message);
        }

        // [Fact]
        public void ThrowExceptionWhenNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => AcmeCertificateExtensions.ToPfx(null));
        }

        // [Fact]
        public void CanGetSetProperties()
        {
            var authz = new AcmeCertificate();
            authz.VerifyGetterSetter(a => a.Revoked, true);
            authz.VerifyGetterSetter(a => a.Issuer, new AcmeCertificate());
        }
    }
}

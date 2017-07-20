using Certes.Acme;
using System;
using Xunit;

namespace Certes.Tests.Acme
{
    public class AcmeCertificateTests
    {
        [Fact]
        public void ThrowExceptionWhenCertDataMissing()
        {
            var cert = new AcmeCertificate
            {
                Location = new Uri("http://my-cert-url.com", UriKind.Absolute),
                Raw = null
            };

            var ex = Assert.Throws<Exception>(() => AcmeCertificateExtensions.ToPfx(cert));
            Assert.True(ex.Message.Contains(cert.Location.AbsoluteUri));
        }
    }
}

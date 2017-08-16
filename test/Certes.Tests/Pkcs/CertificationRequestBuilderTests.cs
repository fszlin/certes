using System;
using Xunit;

namespace Certes.Pkcs
{
    public class CertificationRequestBuilderTests
    {
        [Fact]
        public void CanCreateCsrWithKey()
        {
            var key = Helper.Loadkey();
            new CertificationRequestBuilder(key.Export());
        }

        [Fact]
        public void CanSetSubjectAlternativeNames()
        {
            var san = new[]
            {
                "www.example.com",
                "www1.example.com"
            };

            var csr = new CertificationRequestBuilder()
            {
                SubjectAlternativeNames = san
            };

            Assert.Equal(san, csr.SubjectAlternativeNames);

            Assert.Throws<ArgumentNullException>(() => csr.SubjectAlternativeNames = null);
        }
    }
}

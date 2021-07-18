using System;
using System.Threading.Tasks;
using Xunit;

namespace Certes.Pkcs
{
    public class CertificationRequestBuilderTests
    {
        [Fact]
        public void CanCreateCsrWithSignatureKey()
        {
            var key = KeyFactory.NewKey(KeyAlgorithm.RS256);
            new CertificationRequestBuilder(key);
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

        [Fact]
        public void CanAddAttributes()
        {
            var csr = new CertificationRequestBuilder();
            csr.AddName("st", "yonge street");
            csr.AddName("cn", "www.certes.com");

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                csr.AddName("invalid-name", "omg"));
        }

        [Fact]
        public void CanBuildCsrWithoutSubjectAlternativeName()
        {
            var csr = new CertificationRequestBuilder();
            csr.AddName("cn", "www.example.com");
            var csrData = csr.Generate();
            Assert.NotNull(csrData);
        }
    }
}

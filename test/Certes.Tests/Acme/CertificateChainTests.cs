using System;
using System.IO;
using Xunit;

namespace Certes.Acme
{
    public class CertificateChainTests
    {
        [Fact]
        public void CanGenerateFullChainPem()
        {
            var pem =
                string.Join(Environment.NewLine,
                File.ReadAllText("./Data/leaf-cert.pem").Trim(),
                File.ReadAllText("./Data/test-ca2.pem").Trim(),
                File.ReadAllText("./Data/test-root.pem").Trim());

            var chain = new CertificateChain(pem);
            var result = chain.ToPem();
            Assert.Equal(pem.Replace("\r", "").Trim(), result.Replace("\r", "").Trim());
        }

        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        public void CanGenerateFullChainPemWithKey(KeyAlgorithm algorithm)
        {
            var fixture = new CertificateFixture(algorithm);
            var expectedPem =
                fixture.Key.ToPem().Trim() +
                Environment.NewLine +
                fixture.Chain.ToPem();

            var result = fixture.Chain.ToPem(fixture.Key);
            Assert.Equal(expectedPem.Replace("\r", "").Trim(), result.Replace("\r", "").Trim());
        }

        [Theory]
        [InlineData(KeyAlgorithm.RS256, KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256, KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES256, KeyAlgorithm.ES384)]
        public void RejectsPrivateKeyThatDoesNotMatchLeafCertificate(
            KeyAlgorithm certificateAlgorithm, KeyAlgorithm privateKeyAlgorithm)
        {
            var fixture = new CertificateFixture(certificateAlgorithm);

            var exception = Assert.Throws<AcmeException>(
                () => fixture.Chain.ToPem(KeyFactory.NewKey(privateKeyAlgorithm)));

            Assert.Equal(Properties.Strings.ErrorPrivateKeyMismatch, exception.Message);
        }

        [Fact]
        public void FailWhenMissingIntermediateCert()
        {
            var pem =
                string.Join(Environment.NewLine,
                File.ReadAllText("./Data/leaf-cert.pem").Trim(),
                File.ReadAllText("./Data/test-root.pem").Trim());

            var chain = new CertificateChain(pem);
            Assert.Throws<AcmeException>(() => chain.ToPem());
        }
    }
}

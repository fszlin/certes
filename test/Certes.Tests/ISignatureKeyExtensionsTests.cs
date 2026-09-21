using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Certes.Jws;
using Certes.Crypto;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using Xunit;

namespace Certes
{
    public class ISignatureKeyExtensionsTests
    {
        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        public void TlsAlpnCertificateContainsChallengeProof(KeyAlgorithm algorithm)
        {
            var accountKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var certificateKey = KeyFactory.NewKey(algorithm);
            const string host = "tls-alpn.example.test";
            const string token = "test-challenge-token";
            var pem = accountKey.TlsAlpnCertificate(token, host, certificateKey);
            var certificate = new X509CertificateParser().ReadCertificate(Encoding.UTF8.GetBytes(pem));
            var (_, keyPair) = new KeyAlgorithmProvider().GetKeyPair(certificateKey.ToDer());
            Assert.Equal(keyPair.Public, certificate.GetPublicKey());
            Assert.True(certificate.SubjectDN.Equivalent(certificate.IssuerDN));
            certificate.Verify(certificate.GetPublicKey());

            var names = GeneralNames.GetInstance(X509ExtensionUtilities.FromExtensionValue(
                certificate.GetExtensionValue(X509Extensions.SubjectAlternativeName))).GetNames();
            var name = Assert.Single(names);
            Assert.Equal(GeneralName.DnsName, name.TagNo);
            Assert.Equal(host, DerIA5String.GetInstance(name.Name).GetString());

            var identifier = new DerObjectIdentifier("1.3.6.1.5.5.7.1.31");
            Assert.Contains(identifier.Id, certificate.GetCriticalExtensionOids().Cast<string>());
            // RFC 8737 section 3: extnValue contains a DER OCTET STRING of the digest.
            var digest = Asn1OctetString.GetInstance(X509ExtensionUtilities.FromExtensionValue(
                certificate.GetExtensionValue(identifier))).GetOctets();
            using var sha256 = SHA256.Create();
            Assert.Equal(sha256.ComputeHash(Encoding.UTF8.GetBytes(accountKey.KeyAuthorization(token))), digest);
        }

        [Fact]
        public void CanGenerateDnsRecordValue()
        {
            var key = KeyFactory.NewKey(KeyAlgorithm.ES256);
            using (var sha256 = SHA256.Create())
            {
                Assert.Equal(
                    JwsConvert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(key.KeyAuthorization("token")))),
                    key.DnsTxt("token"));
            }
        }
    }
}

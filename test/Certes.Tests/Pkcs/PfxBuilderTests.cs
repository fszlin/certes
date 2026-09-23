using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Certes.Properties;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Pkcs;
using Xunit;

namespace Certes.Pkcs
{
    public class PfxBuilderTests
    {
        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        public void CanCreatePfxChain(KeyAlgorithm algorithm)
        {
            var fixture = new CertificateFixture(algorithm);
            var pfxBuilder = fixture.Chain.ToPfx(fixture.Key);
            var pfx = pfxBuilder.Build("my-cert", "abcd1234");
            fixture.AssertPfx(pfx, "abcd1234", "my-cert");
        }

        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        [InlineData(KeyAlgorithm.ES384)]
        [InlineData(KeyAlgorithm.ES512)]
        public void CanCreatePfxWithoutChain(KeyAlgorithm algorithm)
        {
            var fixture = new CertificateFixture(algorithm);
            var pfxBuilder = new PfxBuilder(fixture.Leaf.GetEncoded(), fixture.Key);
            pfxBuilder.FullChain = false;
            var pfx = pfxBuilder.Build("my-cert", "abcd1234");
            fixture.AssertPfx(pfx, "abcd1234", "my-cert", fullChain: false);
        }

        [Theory]
        [InlineData(KeyAlgorithm.RS256, KeyAlgorithm.RS256, true)]
        [InlineData(KeyAlgorithm.RS256, KeyAlgorithm.RS256, false)]
        [InlineData(KeyAlgorithm.ES256, KeyAlgorithm.ES256, true)]
        [InlineData(KeyAlgorithm.ES256, KeyAlgorithm.ES256, false)]
        [InlineData(KeyAlgorithm.ES256, KeyAlgorithm.ES384, true)]
        [InlineData(KeyAlgorithm.ES256, KeyAlgorithm.ES384, false)]
        public void RejectsPrivateKeyThatDoesNotMatchLeafCertificate(
            KeyAlgorithm certificateAlgorithm, KeyAlgorithm privateKeyAlgorithm, bool fullChain)
        {
            var fixture = new CertificateFixture(certificateAlgorithm);
            var pfxBuilder = fixture.Chain.ToPfx(KeyFactory.NewKey(privateKeyAlgorithm));
            pfxBuilder.FullChain = fullChain;

            var exception = Assert.Throws<AcmeException>(
                () => pfxBuilder.Build("my-cert", "abcd1234"));

            Assert.Equal(Strings.ErrorPrivateKeyMismatch, exception.Message);
        }

        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        public void CanCreatePfxChainWithoutRoot(KeyAlgorithm algorithm)
        {
            // RFC 8555, section 7.4.2: the server is not expected to supply the self-signed root.
            var fixture = new CertificateFixture(algorithm);
            var chain = CertificateFixture.ChainOf(fixture.Leaf, fixture.Intermediate);

            var pfx = chain.ToPfx(fixture.Key).Build("my-cert", "abcd1234");

            fixture.AssertPfx(pfx, "abcd1234", "my-cert");
            fixture.AssertPfxChain(pfx, "abcd1234", fixture.Leaf, fixture.Intermediate);
        }

        [Fact]
        public void PfxChainExcludesRootWhenSupplied()
        {
            // Relying parties supply their own trust anchors, so the self-signed root is
            // packaged by neither path.
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var pfx = fixture.Chain.ToPfx(fixture.Key).Build("my-cert", "abcd1234");

            fixture.AssertPfxChain(pfx, "abcd1234", fixture.Leaf, fixture.Intermediate);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ExportsExpiredCertificateWithoutValidatingThePath(bool supplyRoot)
        {
            // Export packages a chain; it does not validate a certification path. An expired
            // certificate exports either way. The previous implementation ran PKIX path
            // validation whenever a self-signed root was supplied, which rejected this.
            var fixture = new CertificateFixture(KeyAlgorithm.ES256, expireLeaf: true);
            Assert.True(fixture.Leaf.NotAfter < DateTime.UtcNow);

            var chain = supplyRoot
                ? CertificateFixture.ChainOf(fixture.Leaf, fixture.Intermediate, fixture.Root)
                : CertificateFixture.ChainOf(fixture.Leaf, fixture.Intermediate);

            var pfx = chain.ToPfx(fixture.Key).Build("my-cert", "abcd1234");

            fixture.AssertPfxChain(pfx, "abcd1234", fixture.Leaf, fixture.Intermediate);
            AssertKeyMatchesCertificate(pfx, "abcd1234", fixture.Leaf);
        }

        /// <summary>
        /// Reads the PFX through .NET rather than BouncyCastle, so the assertion does not share
        /// an implementation with the exporter, and checks the key really belongs to the
        /// certificate by signing with it and verifying against the certificate public key.
        /// </summary>
        private static void AssertKeyMatchesCertificate(
            byte[] pfx, string password, Org.BouncyCastle.X509.X509Certificate expectedLeaf)
        {
#if NETFRAMEWORK
            using (var imported = new X509Certificate2(pfx, password, X509KeyStorageFlags.DefaultKeySet))
#else
            using (var imported = X509CertificateLoader.LoadPkcs12(
                pfx, password, X509KeyStorageFlags.DefaultKeySet))
#endif
            {
                Assert.Equal(expectedLeaf.GetEncoded(), imported.RawData);
                Assert.True(imported.HasPrivateKey);

                var data = Encoding.UTF8.GetBytes("certes packaging probe");
                using (var privateKey = imported.GetECDsaPrivateKey())
                using (var publicKey = imported.GetECDsaPublicKey())
                {
                    var signature = privateKey.SignData(data, HashAlgorithmName.SHA256);
                    Assert.True(publicKey.VerifyData(data, signature, HashAlgorithmName.SHA256));
                }
            }
        }

        [Fact]
        public void FullChainExportsLeafWhenNoIssuersAreSupplied()
        {
            // A leaf issued directly by a root the server omitted has no issuers to package.
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var builder = new PfxBuilder(fixture.Leaf.GetEncoded(), fixture.Key);

            var pfx = builder.Build("my-cert", "abcd1234");

            fixture.AssertPfx(pfx, "abcd1234", "my-cert", fullChain: false);
            fixture.AssertPfxChain(pfx, "abcd1234", fixture.Leaf);
        }

        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        public void DefaultEncryptionUsesAes256(KeyAlgorithm algorithm)
        {
            var fixture = new CertificateFixture(algorithm);
            var builder = fixture.Chain.ToPfx(fixture.Key);

            Assert.Equal(PfxEncryption.Aes256, builder.Encryption);
            var pfx = builder.Build("my-cert", "abcd1234");

            var (keyAlgorithm, certAlgorithm) = ReadPfxAlgorithms(pfx);
            AssertPbes2Aes256(keyAlgorithm);
            AssertPbes2Aes256(certAlgorithm);
            fixture.AssertPfx(pfx, "abcd1234", "my-cert");
        }

        [Theory]
        [InlineData(KeyAlgorithm.RS256)]
        [InlineData(KeyAlgorithm.ES256)]
        public void LegacyEncryptionUsesTripleDesAndRc2(KeyAlgorithm algorithm)
        {
            var fixture = new CertificateFixture(algorithm);
            var builder = fixture.Chain.ToPfx(fixture.Key);
            builder.Encryption = PfxEncryption.Legacy;

            var pfx = builder.Build("my-cert", "abcd1234");

            var (keyAlgorithm, certAlgorithm) = ReadPfxAlgorithms(pfx);
            Assert.Equal(PkcsObjectIdentifiers.PbeWithShaAnd3KeyTripleDesCbc, keyAlgorithm.Algorithm);
            Assert.Equal(PkcsObjectIdentifiers.PbewithShaAnd40BitRC2Cbc, certAlgorithm.Algorithm);
            fixture.AssertPfx(pfx, "abcd1234", "my-cert");
        }

        [Fact]
        public void RejectsUnknownEncryption()
        {
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var builder = fixture.Chain.ToPfx(fixture.Key);
            builder.Encryption = (PfxEncryption)42;

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Build("my-cert", "abcd1234"));
        }

        private static void AssertPbes2Aes256(Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier algorithm)
        {
            Assert.Equal(PkcsObjectIdentifiers.IdPbeS2, algorithm.Algorithm);
            var parameters = PbeS2Parameters.GetInstance(algorithm.Parameters);
            Assert.Equal(NistObjectIdentifiers.IdAes256Cbc, parameters.EncryptionScheme.Algorithm);
            Assert.Equal(PkcsObjectIdentifiers.IdPbkdf2, parameters.KeyDerivationFunc.Algorithm);
            var kdf = Pbkdf2Params.GetInstance(parameters.KeyDerivationFunc.Parameters);
            Assert.Equal(PkcsObjectIdentifiers.IdHmacWithSha256, kdf.Prf.Algorithm);
        }

        /// <summary>
        /// Reads the encryption algorithms from the PFX structure: the shrouded key bag and the
        /// encrypted certificate content.
        /// </summary>
        private static (Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier Key, Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier Cert)
            ReadPfxAlgorithms(byte[] pfx)
        {
            Org.BouncyCastle.Asn1.X509.AlgorithmIdentifier key = null, cert = null;
            var authSafe = Pfx.GetInstance(pfx).AuthSafe;
            var safe = AuthenticatedSafe.GetInstance(Asn1OctetString.GetInstance(authSafe.Content).GetOctets());
            foreach (var info in safe.GetContentInfo())
            {
                if (info.ContentType.Equals(PkcsObjectIdentifiers.EncryptedData))
                {
                    cert = EncryptedData.GetInstance(info.Content).EncryptionAlgorithm;
                    continue;
                }

                var bags = Asn1Sequence.GetInstance(Asn1OctetString.GetInstance(info.Content).GetOctets());
                foreach (Asn1Encodable item in bags)
                {
                    var bag = SafeBag.GetInstance(item);
                    if (bag.BagID.Equals(PkcsObjectIdentifiers.Pkcs8ShroudedKeyBag))
                    {
                        key = EncryptedPrivateKeyInfo.GetInstance(bag.BagValue).EncryptionAlgorithm;
                    }
                }
            }

            Assert.NotNull(key);
            Assert.NotNull(cert);
            return (key, cert);
        }
    }
}

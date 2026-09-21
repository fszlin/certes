using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Certes.Crypto;
using Org.BouncyCastle.X509;
using Xunit;

namespace Certes.Pkcs
{
    public class CertificateStoreTests
    {
        [Fact]
        public void GetIssuersStopsAtHighestAvailableIssuer()
        {
            // RFC 8555, section 7.4.2: the server is not expected to supply the self-signed root.
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var store = new CertificateStore();
            store.Add(fixture.Intermediate.GetEncoded());

            var issuers = store.GetIssuers(fixture.Leaf.GetEncoded());

            var issuer = Assert.Single(issuers);
            Assert.Equal(fixture.Intermediate.GetEncoded(), issuer);
        }

        [Fact]
        public void GetIssuersIncludesRootWhenAvailable()
        {
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var store = new CertificateStore();
            store.Add(fixture.Intermediate.GetEncoded());
            store.Add(fixture.Root.GetEncoded());

            var issuers = store.GetIssuers(fixture.Leaf.GetEncoded());

            Assert.Equal(2, issuers.Count);
            Assert.Equal(fixture.Intermediate.GetEncoded(), issuers[0]);
            Assert.Equal(fixture.Root.GetEncoded(), issuers[1]);
        }

        [Fact]
        public void GetIssuersFailsWhenSuppliedIssuersDoNotSignTheCertificate()
        {
            // Issuers are indexed by subject name, which does not establish an issuer
            // relationship. A certificate sharing the issuer's name but holding a different
            // key must not be accepted.
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var unrelated = new CertificateFixture(KeyAlgorithm.ES256);
            Assert.True(fixture.Leaf.IssuerDN.Equivalent(unrelated.Intermediate.SubjectDN));

            var store = new CertificateStore();
            store.Add(unrelated.Intermediate.GetEncoded());

            Assert.Throws<AcmeException>(() => store.GetIssuers(fixture.Leaf.GetEncoded()));
        }

        [Fact]
        public void ToPemFailsWhenSuppliedIssuersDoNotSignTheCertificate()
        {
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var unrelated = new CertificateFixture(KeyAlgorithm.ES256);
            var chain = CertificateFixture.ChainOf(fixture.Leaf, unrelated.Intermediate);

            Assert.Throws<AcmeException>(() => chain.ToPem());
        }

        [Fact]
        public void ToPfxFailsWhenSuppliedIssuersDoNotSignTheCertificate()
        {
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var unrelated = new CertificateFixture(KeyAlgorithm.ES256);
            var chain = CertificateFixture.ChainOf(fixture.Leaf, unrelated.Intermediate);

            Assert.Throws<AcmeException>(() => chain.ToPfx(fixture.Key).Build("my-cert", "abcd1234"));
        }

        [Fact]
        public void GetIssuersFailsWhenImmediateIssuerIsMissing()
        {
            // A gap directly above the leaf means the supplied issuers are not for this chain.
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var store = new CertificateStore();
            store.Add(fixture.Root.GetEncoded());

            Assert.Throws<AcmeException>(() => store.GetIssuers(fixture.Leaf.GetEncoded()));
        }

        [Fact]
        public void GetIssuersReturnsNothingWhenNoIssuersAreSupplied()
        {
            // A leaf issued directly by a root that the server omitted has no issuers to
            // return, which is not an error.
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);

            Assert.Empty(new CertificateStore().GetIssuers(fixture.Leaf.GetEncoded()));
        }

        [Fact]
        public void GetIssuersReturnsNothingForSelfSignedCertificate()
        {
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var store = new CertificateStore();
            store.Add(fixture.Root.GetEncoded());

            Assert.Empty(store.GetIssuers(fixture.Root.GetEncoded()));
        }

        [Fact]
        public void GetIssuersReturnsVerifiedAcyclicPrefixForMutuallyIssuedCertificates()
        {
            // Issuers are indexed by subject name, so a pair of certificates naming each other
            // as issuer forms a cycle. The walk must stop and return the verified prefix built
            // before the cycle repeats, never revisiting a subject name.
            var provider = new KeyAlgorithmProvider();
            var (_, keyPair) = provider.GetKeyPair(KeyFactory.NewKey(KeyAlgorithm.RS256).ToDer());
            var now = DateTime.UtcNow;

            var first = CertificateFixture.Issue(
                "CN=Certes Cycle A", "CN=Certes Cycle B", keyPair.Public, keyPair.Private, true, 11, now);
            var second = CertificateFixture.Issue(
                "CN=Certes Cycle B", "CN=Certes Cycle A", keyPair.Public, keyPair.Private, true, 12, now);
            var leaf = CertificateFixture.Issue(
                "CN=cycle.example", "CN=Certes Cycle A", keyPair.Public, keyPair.Private, false, 13, now);

            var store = new CertificateStore();
            store.Add(first.GetEncoded());
            store.Add(second.GetEncoded());

            var parser = new X509CertificateParser();
            var issuers = store.GetIssuers(leaf.GetEncoded())
                .Select(der => parser.ReadCertificate(der))
                .ToList();

            // Terminates, and no subject name appears twice.
            Assert.Equal(
                issuers.Select(c => c.SubjectDN).Distinct().Count(),
                issuers.Count);

            // Every returned issuer signed the certificate below it.
            var child = leaf;
            foreach (var issuer in issuers)
            {
                child.Verify(issuer.GetPublicKey());
                child = issuer;
            }

            Assert.Equal(new[] { first.GetEncoded(), second.GetEncoded() }, issuers.Select(c => c.GetEncoded()));
        }

        [Fact]
        public void ToPemStopsAtHighestAvailableIssuer()
        {
            var fixture = new CertificateFixture(KeyAlgorithm.ES256);
            var chain = CertificateFixture.ChainOf(fixture.Leaf, fixture.Intermediate);

            var parser = new X509CertificateParser();
            var exported = parser.ReadCertificates(
                System.Text.Encoding.UTF8.GetBytes(chain.ToPem())).OfType<X509Certificate>().ToList();

            Assert.Equal(2, exported.Count);
            Assert.Equal(fixture.Leaf.GetEncoded(), exported[0].GetEncoded());
            Assert.Equal(fixture.Intermediate.GetEncoded(), exported[1].GetEncoded());
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetIssuersPrefersSelfSignedCrossSignedAlternate(bool addSelfSignedFirst)
        {
            // The self-signed and cross-signed alternates share a subject name and a key, so
            // both verify the leaf. Selection must not depend on which was added last.
            var pki = new CrossSignedPki();
            var store = new CertificateStore();
            if (addSelfSignedFirst)
            {
                store.Add(pki.SelfSignedIssuer.GetEncoded());
                store.Add(pki.CrossSignedIssuer.GetEncoded());
            }
            else
            {
                store.Add(pki.CrossSignedIssuer.GetEncoded());
                store.Add(pki.SelfSignedIssuer.GetEncoded());
            }

            store.Add(pki.CrossSigningRoot.GetEncoded());

            var issuers = store.GetIssuers(pki.Leaf.GetEncoded());

            // The short chain ends at the self-signed alternate.
            var issuer = Assert.Single(issuers);
            Assert.Equal(pki.SelfSignedIssuer.GetEncoded(), issuer);
        }

        [Fact]
        public void GetIssuersUsesCrossSignedAlternateWhenItIsTheOnlyOneSupplied()
        {
            var pki = new CrossSignedPki();
            var store = new CertificateStore();
            store.Add(pki.CrossSignedIssuer.GetEncoded());
            store.Add(pki.CrossSigningRoot.GetEncoded());

            var issuers = store.GetIssuers(pki.Leaf.GetEncoded());

            Assert.Equal(
                new[] { pki.CrossSignedIssuer.GetEncoded(), pki.CrossSigningRoot.GetEncoded() },
                issuers);
        }

        [Fact]
        public void GetIssuersIsNotShadowedBySameSubjectImposter()
        {
            // A certificate sharing the issuer's subject name but holding a different key must
            // not displace the real issuer, whichever order they are added in.
            var pki = new CrossSignedPki();
            var store = new CertificateStore();
            store.Add(pki.CrossSignedIssuer.GetEncoded());
            store.Add(pki.SameSubjectImposter.GetEncoded());
            store.Add(pki.CrossSigningRoot.GetEncoded());

            var issuers = store.GetIssuers(pki.Leaf.GetEncoded());

            Assert.Equal(
                new[] { pki.CrossSignedIssuer.GetEncoded(), pki.CrossSigningRoot.GetEncoded() },
                issuers);
        }

        [Fact]
        public void AddingTheSameCertificateTwiceDoesNotAffectTheChain()
        {
            var pki = new CrossSignedPki();
            var store = new CertificateStore();
            store.Add(pki.CrossSignedIssuer.GetEncoded());
            store.Add(pki.CrossSignedIssuer.GetEncoded());
            store.Add(pki.CrossSigningRoot.GetEncoded());

            Assert.Equal(2, store.GetIssuers(pki.Leaf.GetEncoded()).Count);
        }

        [Fact]
        public void GetIssuersPrefersValidAlternateOverExpiredSelfSignedOne()
        {
            // An expired self-signed alternate must not be chosen over a usable cross-signed
            // path. Serving an expired root is what broke older clients when DST Root CA X3
            // expired in 2021.
            var pki = new CrossSignedPki(expireSelfSignedIssuer: true);
            var store = new CertificateStore();
            store.Add(pki.SelfSignedIssuer.GetEncoded());
            store.Add(pki.CrossSignedIssuer.GetEncoded());
            store.Add(pki.CrossSigningRoot.GetEncoded());

            var issuers = store.GetIssuers(pki.Leaf.GetEncoded());

            Assert.Equal(
                new[] { pki.CrossSignedIssuer.GetEncoded(), pki.CrossSigningRoot.GetEncoded() },
                issuers);
        }

        [Fact]
        public void SuppliedIssuerTakesPrecedenceOverEmbeddedRoot()
        {
            // An embedded root must not displace an explicitly supplied cross-signed alternate
            // of the same subject and key. Synthetic subject names cannot expose this, so this
            // uses a subject/key that the library actually embeds.
            var embeddedRoot = LoadEmbeddedCertificate("fake-le-root-x1.pem");
            var intermediate = new X509CertificateParser()
                .ReadCertificate(File.ReadAllBytes("./Data/fake-le-intermediate-x1.pem"));
            Assert.True(intermediate.IssuerDN.Equivalent(embeddedRoot.SubjectDN));

            var provider = new KeyAlgorithmProvider();
            var (_, ephemeralRootKey) = provider.GetKeyPair(KeyFactory.NewKey(KeyAlgorithm.RS256).ToDer());
            var now = DateTime.UtcNow;
            const string ephemeralRootName = "CN=Certes Ephemeral Cross Signing Root";

            var ephemeralRoot = CertificateFixture.Issue(
                ephemeralRootName, ephemeralRootName,
                ephemeralRootKey.Public, ephemeralRootKey.Private, true, 41, now);

            // Same subject and key as the embedded root, but signed by the ephemeral root.
            var crossSigned = CertificateFixture.Issue(
                embeddedRoot.SubjectDN.ToString(), ephemeralRootName,
                embeddedRoot.GetPublicKey(), ephemeralRootKey.Private, true, 42, now);

            // Both the embedded root and the cross-signed variant verify the intermediate.
            intermediate.Verify(embeddedRoot.GetPublicKey());
            intermediate.Verify(crossSigned.GetPublicKey());
            crossSigned.Verify(ephemeralRoot.GetPublicKey());

            var store = new CertificateStore();
            store.Add(crossSigned.GetEncoded());
            store.Add(ephemeralRoot.GetEncoded());

            var issuers = store.GetIssuers(intermediate.GetEncoded());

            Assert.Equal(
                new[] { crossSigned.GetEncoded(), ephemeralRoot.GetEncoded() },
                issuers);
            Assert.DoesNotContain(embeddedRoot.GetEncoded(), issuers);
        }

        [Fact]
        public void EmbeddedRootIsUsedWhenNoSuppliedIssuerServes()
        {
            // The embedded fallback still applies when nothing supplied can act as the issuer.
            var embeddedRoot = LoadEmbeddedCertificate("fake-le-root-x1.pem");
            var intermediate = new X509CertificateParser()
                .ReadCertificate(File.ReadAllBytes("./Data/fake-le-intermediate-x1.pem"));

            var issuers = new CertificateStore().GetIssuers(intermediate.GetEncoded());

            var issuer = Assert.Single(issuers);
            Assert.Equal(embeddedRoot.GetEncoded(), issuer);
        }

        private static X509Certificate LoadEmbeddedCertificate(string name)
        {
            var assembly = typeof(PfxBuilder).GetTypeInfo().Assembly;
            var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith(name));
            using (var stream = assembly.GetManifestResourceStream(resource))
            {
                return new X509CertificateParser().ReadCertificate(stream);
            }
        }

        /// <summary>
        /// A cross-signed issuer: one subject name and key, published both self-signed and
        /// signed by a second root, mirroring the ISRG Root X1 arrangement.
        /// </summary>
        private sealed class CrossSignedPki
        {
            public X509Certificate CrossSigningRoot { get; }
            public X509Certificate SelfSignedIssuer { get; }
            public X509Certificate CrossSignedIssuer { get; }
            public X509Certificate SameSubjectImposter { get; }
            public X509Certificate Leaf { get; }

            public CrossSignedPki(bool expireSelfSignedIssuer = false)
            {
                var provider = new KeyAlgorithmProvider();
                var (_, rootKey) = provider.GetKeyPair(KeyFactory.NewKey(KeyAlgorithm.RS256).ToDer());
                var (_, issuerKey) = provider.GetKeyPair(KeyFactory.NewKey(KeyAlgorithm.RS256).ToDer());
                var (_, otherKey) = provider.GetKeyPair(KeyFactory.NewKey(KeyAlgorithm.RS256).ToDer());
                var (_, leafKey) = provider.GetKeyPair(KeyFactory.NewKey(KeyAlgorithm.RS256).ToDer());
                var now = DateTime.UtcNow;

                // Issue() sets notAfter 30 days after the supplied instant.
                var selfSignedIssuedAt = expireSelfSignedIssuer ? now.AddDays(-400) : now;

                const string rootName = "CN=Certes Cross Signing Root";
                const string issuerName = "CN=Certes Cross Signed Issuer";

                CrossSigningRoot = CertificateFixture.Issue(
                    rootName, rootName, rootKey.Public, rootKey.Private, true, 21, now);
                SelfSignedIssuer = CertificateFixture.Issue(
                    issuerName, issuerName, issuerKey.Public, issuerKey.Private, true, 22, selfSignedIssuedAt);
                CrossSignedIssuer = CertificateFixture.Issue(
                    issuerName, rootName, issuerKey.Public, rootKey.Private, true, 23, now);
                SameSubjectImposter = CertificateFixture.Issue(
                    issuerName, issuerName, otherKey.Public, otherKey.Private, true, 24, now);
                Leaf = CertificateFixture.Issue(
                    "CN=cross.example", issuerName, leafKey.Public, issuerKey.Private, false, 25, now);
            }
        }
    }
}

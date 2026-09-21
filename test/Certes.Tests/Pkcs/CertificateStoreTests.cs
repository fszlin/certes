using System;
using System.Linq;
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
        public void GetIssuersTerminatesOnMutuallyIssuedCertificates()
        {
            // Issuers are indexed by subject name, so a pair of certificates naming each other
            // as issuer forms a cycle. The walk must stop rather than loop.
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

            var issuers = store.GetIssuers(leaf.GetEncoded());

            Assert.Equal(2, issuers.Count);
            Assert.Equal(first.GetEncoded(), issuers[0]);
            Assert.Equal(second.GetEncoded(), issuers[1]);
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
    }
}

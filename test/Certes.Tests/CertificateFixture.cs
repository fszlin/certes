using System;
using System.IO;
using System.Linq;
using Certes.Acme;
using Certes.Crypto;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Xunit;

namespace Certes
{
    // Ephemeral test-only PKI. No CA services, persisted private keys, or OS trust changes.
    internal sealed class CertificateFixture
    {
        public IKey Key { get; }
        public X509Certificate Leaf { get; }
        public X509Certificate Intermediate { get; }
        public X509Certificate Root { get; }
        public CertificateChain Chain { get; }

        public CertificateFixture(KeyAlgorithm algorithm, bool expireLeaf = false)
        {
            var provider = new KeyAlgorithmProvider();
            Key = KeyFactory.NewKey(algorithm);
            var (_, leafKey) = provider.GetKeyPair(Key.ToDer());
            var (_, rootKey) = provider.GetKeyPair(KeyFactory.NewKey(KeyAlgorithm.RS256).ToDer());
            var (_, intermediateKey) = provider.GetKeyPair(KeyFactory.NewKey(KeyAlgorithm.RS256).ToDer());
            var now = DateTime.UtcNow;

            Root = Issue("CN=Certes Unit Test Root", "CN=Certes Unit Test Root", rootKey.Public, rootKey.Private, true, 1, now);
            Intermediate = Issue("CN=Certes Unit Test Intermediate", Root.SubjectDN.ToString(), intermediateKey.Public, rootKey.Private, true, 2, now);
            // Issue() sets notAfter 30 days after the supplied instant.
            var leafIssuedAt = expireLeaf ? now.AddDays(-400) : now;
            Leaf = Issue("CN=unit.example", Intermediate.SubjectDN.ToString(), leafKey.Public, intermediateKey.Private, false, 3, leafIssuedAt);

            Leaf.Verify(Intermediate.GetPublicKey());
            Intermediate.Verify(Root.GetPublicKey());

            using (var text = new StringWriter())
            {
                var writer = new PemWriter(text);
                writer.WriteObject(Leaf);
                writer.WriteObject(Intermediate);
                writer.WriteObject(Root);
                Chain = new CertificateChain(text.ToString());
            }
        }

        /// <summary>
        /// Builds a chain from the given certificates, leaf first.
        /// </summary>
        public static CertificateChain ChainOf(params X509Certificate[] certificates)
        {
            using (var text = new StringWriter())
            {
                var writer = new PemWriter(text);
                foreach (var certificate in certificates)
                {
                    writer.WriteObject(certificate);
                }

                return new CertificateChain(text.ToString());
            }
        }

        /// <summary>
        /// Asserts the exact certificate chain stored against the key entry, in order.
        /// </summary>
        public void AssertPfxChain(byte[] pfx, string password, params X509Certificate[] expected)
        {
            using (var stream = new MemoryStream(pfx))
            {
                var store = new Pkcs12Store(stream, password.ToCharArray());
                var alias = store.Aliases.Cast<string>().Single(store.IsKeyEntry);
                var chain = store.GetCertificateChain(alias);

                Assert.Equal(
                    expected.Select(c => c.GetEncoded()),
                    chain.Select(c => c.Certificate.GetEncoded()));
            }
        }

        public string AssertPfx(byte[] pfx, string password, string alias, bool fullChain = true)
        {
            using (var stream = new MemoryStream(pfx))
            {
                var store = new Pkcs12Store(stream, password.ToCharArray());
                var keyAliases = store.Aliases.Cast<string>().Where(store.IsKeyEntry).ToArray();
                var actualAlias = Assert.Single(keyAliases);
                if (alias != null)
                {
                    Assert.Equal(alias, actualAlias);
                }

                Assert.Equal(Key.ToDer(), PrivateKeyInfoFactory.CreatePrivateKeyInfo(store.GetKey(actualAlias).Key).GetDerEncoded());
                Assert.Equal(Leaf.GetEncoded(), store.GetCertificate(actualAlias).Certificate.GetEncoded());
                var chain = store.GetCertificateChain(actualAlias);
                Assert.Equal(Leaf.GetEncoded(), chain[0].Certificate.GetEncoded());
                if (fullChain)
                {
                    Assert.Contains(chain, c => c.Certificate.Equals(Intermediate));
                }
                else
                {
                    Assert.Single(chain);
                }

                return actualAlias;
            }
        }

        internal static X509Certificate Issue(string subject, string issuer, AsymmetricKeyParameter publicKey,
            AsymmetricKeyParameter signingKey, bool isCa, int serial, DateTime now)
        {
            var generator = new X509V3CertificateGenerator();
            generator.SetSerialNumber(BigInteger.ValueOf(serial));
            generator.SetSubjectDN(new X509Name(subject));
            generator.SetIssuerDN(new X509Name(issuer));
            generator.SetPublicKey(publicKey);
            generator.SetNotBefore(now.AddDays(-1));
            generator.SetNotAfter(now.AddDays(30));
            generator.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(isCa));
            generator.AddExtension(X509Extensions.KeyUsage, true,
                new KeyUsage(isCa ? KeyUsage.KeyCertSign | KeyUsage.CrlSign : KeyUsage.DigitalSignature));
            if (!isCa)
            {
                generator.AddExtension(X509Extensions.SubjectAlternativeName, false,
                    new GeneralNames(new GeneralName(GeneralName.DnsName, "unit.example")));
            }

            return generator.Generate(new Asn1SignatureFactory("SHA256WITHRSA", signingKey, new SecureRandom()));
        }
    }
}

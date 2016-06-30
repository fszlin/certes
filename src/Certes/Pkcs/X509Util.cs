using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Certes.Pkcs
{
    public static class X509Util
    {
        public static byte[] BuildPfx(byte[] certificate, KeyInfo privateKeyInfo, string friendlyName, string password, bool fullChain = true)
        {
            var keyPair = privateKeyInfo.CreateKeyPair();

            var store = new Pkcs12StoreBuilder().Build();

            if (fullChain)
            {
                var certChain = FindChain(certificate);
                var certChainEntries = certChain.Select(c => new X509CertificateEntry(c)).ToArray();

                store.SetCertificateEntry(friendlyName, certChainEntries.Last());
                store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(keyPair.Private), certChainEntries);
            }
            else
            {
                var certParser = new X509CertificateParser();
                var targetCert = certParser.ReadCertificate(certificate);
                var entry = new X509CertificateEntry(targetCert);

                store.SetCertificateEntry(friendlyName, entry);
                store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(keyPair.Private), new[] { entry });
            }

            using (var buffer = new MemoryStream())
            {
                store.Save(buffer, password.ToCharArray(), new SecureRandom());
                return buffer.ToArray();
            }
        }

        private static IList<X509Certificate> FindChain(byte[] leafDer)
        {
            var certParser = new X509CertificateParser();
            var assembly = typeof(X509Util).GetTypeInfo().Assembly;

            // TODO: Read "up" links for issuer certificates
            var certificates = assembly
                .GetManifestResourceNames()
                .Where(n => n.EndsWith(".cer"))
                .Select(n =>
                {
                    using (var stream = assembly.GetManifestResourceStream(n))
                    {
                        var cert = certParser.ReadCertificate(stream);
                        return new
                        {
                            IsRoot = cert.IssuerDN.Equivalent(cert.SubjectDN),
                            Cert = cert
                        };
                    }
                })
                .ToArray();

            var targetCert = certParser.ReadCertificate(leafDer);
            var rootCerts = new HashSet(certificates.Where(c => c.IsRoot).Select(c => new TrustAnchor(c.Cert, null)));
            var intermediateCerts = certificates.Where(c => !c.IsRoot).Select(c => c.Cert).ToList();
            intermediateCerts.Add(targetCert);
            var target = new X509CertStoreSelector()
            {
                Certificate = targetCert
            };

            var builderParams = new PkixBuilderParameters(rootCerts, target)
            {
                IsRevocationEnabled = false
            };

            builderParams.AddStore(
                X509StoreFactory.Create(
                    "Certificate/Collection",
                    new X509CollectionStoreParameters(intermediateCerts)));

            var builder = new PkixCertPathBuilder();
            var result = builder.Build(builderParams);

            var fullChain = result.CertPath.Certificates.Cast<X509Certificate>().ToList();
            fullChain.Add(targetCert);
            return fullChain;
        }
    }
}

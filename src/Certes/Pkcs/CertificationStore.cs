using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;

namespace Certes.Pkcs
{
    /// <summary>
    /// Represents a collection of X509 certificates.
    /// </summary>
    public class CertificateStore
    {
        private readonly Dictionary<X509Name, X509Certificate> certificates = new Dictionary<X509Name, X509Certificate>();

        private readonly Lazy<X509Certificate[]> embeddedRootCertificates = new Lazy<X509Certificate[]>(() =>
        {
            var certParser = new X509CertificateParser();
            var assembly = typeof(PfxBuilder).GetTypeInfo().Assembly;
            return assembly
                .GetManifestResourceNames()
                .Where(n => n.EndsWith(".pem"))
                .Select(n =>
                {
                    using (var stream = assembly.GetManifestResourceStream(n))
                    {
                        return certParser.ReadCertificate(stream);
                    }
                })
                .ToArray();
        }, true);

        /// <summary>
        /// Adds issuer certificates.
        /// </summary>
        /// <param name="certificates">The issuer certificates.</param>
        public void Add(byte[] certificates)
        {
            var certParser = new X509CertificateParser();
            var issuers = certParser.ReadCertificates(certificates).OfType<X509Certificate>();
            foreach (var cert in issuers)
            {
                this.certificates[cert.SubjectDN] = cert;
            }
        }

        /// <summary>
        /// Gets the issuers of given certificate.
        /// </summary>
        /// <param name="der">The certificate.</param>
        /// <returns>
        /// The issuers of the certificate.
        /// </returns>
        public IList<byte[]> GetIssuers(byte[] der)
        {
            var certParser = new X509CertificateParser();
            var certificate = certParser.ReadCertificate(der);

            var certificates =
                embeddedRootCertificates.Value.Union(this.certificates.Values)
                .Select(cert => new
                {
                    IsRoot = cert.IssuerDN.Equivalent(cert.SubjectDN),
                    Cert = cert
                });

            var rootCerts = new HashSet(
                certificates
                    .Where(c => c.IsRoot)
                    .Select(c => new TrustAnchor(c.Cert, null)));
            var intermediateCerts = certificates.Where(c => !c.IsRoot).Select(c => c.Cert).ToList();
            intermediateCerts.Add(certificate);

            var target = new X509CertStoreSelector()
            {
                Certificate = certificate
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

            var fullChain = result.CertPath.Certificates.Cast<X509Certificate>();
            return fullChain.Select(cert => cert.GetEncoded()).ToArray();
        }
    }
}

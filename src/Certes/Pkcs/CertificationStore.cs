using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Certes.Properties;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;

namespace Certes.Pkcs
{
    /// <summary>
    /// Represents a collection of X509 certificates.
    /// </summary>
    public class CertificateStore
    {
        private readonly Dictionary<X509Name, X509Certificate> certificates = new Dictionary<X509Name, X509Certificate>();

        private readonly Lazy<Dictionary<X509Name, X509Certificate>> embeddedCertificates = new Lazy<Dictionary<X509Name, X509Certificate>>(() =>
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
                .ToDictionary(c => c.SubjectDN, c => c);
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
        /// The issuers of the certificate, ordered from the closest issuer upwards. The chain
        /// stops at the first self-signed certificate, or at the highest issuer available when
        /// no self-signed certificate is present. ACME servers are not expected to supply the
        /// self-signed root (RFC 8555, section 7.4.2), so a chain that ends at an intermediate
        /// is not an error.
        /// </returns>
        public IList<byte[]> GetIssuers(byte[] der)
        {
            var certParser = new X509CertificateParser();
            var certificate = certParser.ReadCertificate(der);

            var chain = new List<X509Certificate>();
            var visited = new HashSet<X509Name>() { certificate.SubjectDN };
            while (!certificate.SubjectDN.Equivalent(certificate.IssuerDN))
            {
                if (!certificates.TryGetValue(certificate.IssuerDN, out var issuer) &&
                    !embeddedCertificates.Value.TryGetValue(certificate.IssuerDN, out issuer))
                {
                    if (chain.Count == 0)
                    {
                        // Nothing could be linked to the certificate at all, which means the
                        // supplied issuers do not belong to this chain.
                        throw new AcmeException(
                            string.Format(Strings.ErrorIssuerNotFound, certificate.IssuerDN, certificate.SubjectDN));
                    }

                    // The chain is complete up to the highest issuer available. ACME servers are
                    // not expected to supply the self-signed root (RFC 8555, section 7.4.2).
                    break;
                }

                if (!visited.Add(issuer.SubjectDN))
                {
                    // Cross-signed certificates can reference each other by subject name.
                    // Stop rather than walking the cycle indefinitely.
                    break;
                }

                chain.Add(issuer);
                certificate = issuer;
            }

            return chain.Select(cert => cert.GetEncoded()).ToArray();
        }
    }
}

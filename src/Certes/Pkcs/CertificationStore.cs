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
        private readonly Dictionary<X509Name, List<X509Certificate>> certificates = new Dictionary<X509Name, List<X509Certificate>>();

        private readonly Lazy<Dictionary<X509Name, List<X509Certificate>>> embeddedCertificates = new Lazy<Dictionary<X509Name, List<X509Certificate>>>(() =>
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
                .GroupBy(c => c.SubjectDN)
                .ToDictionary(g => g.Key, g => g.ToList());
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
                // Cross-signed certificates share a subject name, so alternates are retained
                // instead of replacing each other. The issuer is chosen when the chain is built.
                if (!this.certificates.TryGetValue(cert.SubjectDN, out var alternates))
                {
                    alternates = new List<X509Certificate>();
                    this.certificates.Add(cert.SubjectDN, alternates);
                }

                if (!alternates.Any(existing => existing.Equals(cert)))
                {
                    alternates.Add(cert);
                }
            }
        }

        /// <summary>
        /// Gets the issuers of given certificate.
        /// </summary>
        /// <param name="der">The certificate.</param>
        /// <returns>
        /// The issuers of the certificate, ordered from the closest issuer upwards. Each issuer
        /// must have signed the certificate below it. The chain stops at the first self-signed
        /// certificate, or at the highest issuer available when no self-signed certificate is
        /// present: ACME servers are not expected to supply the self-signed root (RFC 8555,
        /// section 7.4.2), so a chain that ends at an intermediate is not an error.
        /// </returns>
        /// <exception cref="AcmeException">
        /// Issuers were supplied, but none of them signed the certificate.
        /// </exception>
        public IList<byte[]> GetIssuers(byte[] der)
        {
            var certParser = new X509CertificateParser();
            var certificate = certParser.ReadCertificate(der);

            var chain = new List<X509Certificate>();
            var visited = new HashSet<X509Name>() { certificate.SubjectDN };
            while (!certificate.SubjectDN.Equivalent(certificate.IssuerDN))
            {
                if (!TryGetIssuer(certificate, out var issuer))
                {
                    if (chain.Count == 0 && certificates.Count > 0)
                    {
                        // Issuers were supplied but none of them signed the certificate, so the
                        // caller supplied issuers that do not belong to this chain.
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

        /// <summary>
        /// Finds the certificate that signed <paramref name="certificate"/>. Candidates are
        /// indexed by subject name, which does not establish an issuer relationship, so the
        /// signature is verified before a candidate is accepted.
        /// </summary>
        /// <remarks>
        /// Cross-signed alternates share a subject name and a key, so more than one candidate
        /// can verify the signature. Candidates that are currently within their validity period
        /// are preferred, so an expired alternate is not chosen over a usable one. Among equally
        /// usable candidates a self-signed alternate is preferred, which ends the chain at that
        /// certificate rather than continuing through its cross-signing issuer. Remaining ties
        /// keep the order the certificates were added in.
        /// <para>
        /// This is issuer selection for packaging, not path validation: issuer constraints and
        /// the validity of the complete path are not evaluated here.
        /// </para>
        /// </remarks>
        private bool TryGetIssuer(X509Certificate certificate, out X509Certificate issuer)
        {
            var now = DateTime.UtcNow;
            issuer = GetCandidates(certificate.IssuerDN)
                .Where(candidate => HasSigned(candidate, certificate))
                .OrderByDescending(candidate => candidate.IsValid(now))
                .ThenByDescending(candidate => candidate.SubjectDN.Equivalent(candidate.IssuerDN))
                .FirstOrDefault();

            return issuer != null;
        }

        private IEnumerable<X509Certificate> GetCandidates(X509Name subject)
        {
            if (certificates.TryGetValue(subject, out var supplied))
            {
                foreach (var candidate in supplied)
                {
                    yield return candidate;
                }
            }

            if (embeddedCertificates.Value.TryGetValue(subject, out var embedded))
            {
                foreach (var candidate in embedded)
                {
                    yield return candidate;
                }
            }
        }

        private static bool HasSigned(X509Certificate issuer, X509Certificate certificate)
        {
            try
            {
                certificate.Verify(issuer.GetPublicKey());
                return true;
            }
            catch (Exception)
            {
                // The candidate shares the issuer's subject name but did not sign this
                // certificate; it is not a usable issuer.
                return false;
            }
        }
    }
}

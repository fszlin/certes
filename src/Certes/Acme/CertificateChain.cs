using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Org.BouncyCastle.X509;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the certificate chain downloaded from ACME server.
    /// </summary>
    public class CertificateChain
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateChain"/> class.
        /// </summary>
        /// <param name="certificateChain">The certificate chain.</param>
        public CertificateChain(string certificateChain)
        {
            var certificates = certificateChain
                .Split(new[] { "-----END CERTIFICATE-----" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c + "-----END CERTIFICATE-----");

            Certificate = new CertificateContent(certificates.First());
            Issuers = certificates.Skip(1).Select(c => new CertificateContent(c)).ToArray();
        }

        /// <summary>
        /// Gets or sets the certificate.
        /// </summary>
        /// <value>
        /// The certificate.
        /// </value>
        public IEncodable Certificate { get; }

        /// <summary>
        /// Gets or sets the issuers.
        /// </summary>
        /// <value>
        /// The issuers.
        /// </value>
        public IList<IEncodable> Issuers { get; }

        /// <summary>
        /// Checks if the certificate chain is signed by a preferred issuer.
        /// </summary>
        /// <param name="preferredChain">The name of the preferred issuer</param>
        /// <returns>true if a certificate in the chain is issued by preferredChain or preferredChain is empty</returns>
        public bool MatchesPreferredChain(string preferredChain)
        {
            if (string.IsNullOrEmpty(preferredChain))
                return true;

            var certParser = new X509CertificateParser();
            var allcerts = Issuers.Select(x => x.ToPem()).ToList();
            allcerts.Insert(0, Certificate.ToPem());
            foreach (var pem in allcerts)
            {
                var cert = certParser.ReadCertificate(Encoding.UTF8.GetBytes(pem));
                if (cert.IssuerDN.GetValueList().Contains(preferredChain))
                    return true;
            }

            return false;
        }
    }

}

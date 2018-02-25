using System;
using System.Collections.Generic;
using System.Linq;

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
    }

}

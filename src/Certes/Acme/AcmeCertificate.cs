using System;
using Certes.Pkcs;
using Certes.Properties;

namespace Certes.Acme
{
    /// <summary>
    /// Represents a ACME <see cref="CertificateEntity"/>.
    /// </summary>
    public class AcmeCertificate : KeyedAcmeResult<string>
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="AcmeCertificate"/> is revoked.
        /// </summary>
        /// <value>
        ///   <c>true</c> if revoked; otherwise, <c>false</c>.
        /// </value>
        public bool Revoked { get; set; }

        /// <summary>
        /// Gets or sets the issuer certificate.
        /// </summary>
        /// <value>
        /// The issuer certificate.
        /// </value>
        public AcmeCertificate Issuer { get; set; }
    }

    /// <summary>
    /// Helper methods for <see cref="AcmeCertificate"/>.
    /// </summary>
    public static class AcmeCertificateExtensions
    {
        /// <summary>
        /// Converts the certificate To the PFX builder.
        /// </summary>
        /// <param name="cert">The certificate.</param>
        /// <returns>The PFX builder.</returns>
        /// <exception cref="System.Exception">If the certificate data is missing.</exception>
        public static PfxBuilder ToPfx(this AcmeCertificate cert)
        {
            if ((cert ?? throw new ArgumentNullException(nameof(cert))).Raw == null)
            {
                throw new AcmeException(
                    string.Format(Strings.ErrorMissingCertificateData, cert.Location));
            }

            var pfxBuilder = new PfxBuilder(cert.Raw, cert.Key);
            var issuer = cert.Issuer;
            while (issuer != null)
            {
                pfxBuilder.AddIssuer(issuer.Raw);
                issuer = issuer.Issuer;
            }

            return pfxBuilder;
        }
    }
}

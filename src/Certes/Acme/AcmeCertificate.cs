using Certes.Pkcs;
using System;

namespace Certes.Acme
{
    /// <summary>
    /// Represents a ACME <see cref="Certificate"/>.
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
                throw new Exception($"Certificate data missing, please fetch the certificate from ${cert.Location}");
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

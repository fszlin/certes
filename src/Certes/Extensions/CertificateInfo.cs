using System.Text;
using Certes.Pkcs;
using Org.BouncyCastle.X509;

namespace Certes
{
    /// <summary>
    /// Represents the certificate with private key.
    /// </summary>
    public class CertificateInfo
    {
        private readonly string pem;

        /// <summary>
        /// Gets the private key of the certificate.
        /// </summary>
        /// <value>
        /// The private key of the certificate.
        /// </value>
        public IKey PrivateKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateInfo" /> class.
        /// </summary>
        /// <param name="pem">The certificate in PEM.</param>
        /// <param name="privateKey">The private key.</param>
        public CertificateInfo(string pem, IKey privateKey)
        {
            this.pem = pem;
            PrivateKey = privateKey;
        }

        /// <summary>
        /// Creates PFX for this certificate.
        /// </summary>
        /// <param name="friendlyName">The friendly name for the PFX.</param>
        /// <param name="password">The password for the PFX.</param>
        /// <param name="fullChain">Whether to include the full certificate chain in the PFX.</param>
        /// <param name="issuers">The additional issuers for building the certificate chain.</param>
        /// <returns>
        /// The PFX created.
        /// </returns>
        public byte[] ToPfx(string friendlyName, string password, bool fullChain = true, byte[] issuers = null)
        {
            var pfxBuilder = new PfxBuilder(Encoding.UTF8.GetBytes(pem), PrivateKey);
            pfxBuilder.FullChain = fullChain;
            if (issuers != null)
            {
                pfxBuilder.AddIssuers(issuers);
            }

            return pfxBuilder.Build(friendlyName, password);
        }

        /// <summary>
        /// Exports the certificate to DER.
        /// </summary>
        /// <returns>The DER encoded certificate.</returns>
        public byte[] ToDer()
        {
            var certParser = new X509CertificateParser();
            var cert = certParser.ReadCertificate(Encoding.UTF8.GetBytes(pem));
            return cert.GetEncoded();
        }

        /// <summary>
        /// Exports the certificate to PEM.
        /// </summary>
        /// <returns>The certificate.</returns>
        public string ToPem() => pem;
    }
}

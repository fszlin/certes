using System;
using Certes.Acme;

namespace Certes
{
    /// <summary>
    /// Represents the certificate with private key.
    /// </summary>
    public class CertificateInfo
    {
        /// <summary>
        /// Gets the certificate chain.
        /// </summary>
        /// <value>
        /// The certificate chain.
        /// </value>
        public CertificateChain Chain { get; }

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
        /// <param name="certificateChain">The certificate chain.</param>
        /// <param name="privateKey">The private key.</param>
        public CertificateInfo(CertificateChain certificateChain, IKey privateKey)
        {
            Chain = certificateChain;
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
            if (PrivateKey == null)
            {
                throw new InvalidOperationException("Private key not avaliable.");
            }

            var pfxBuilder = Chain.ToPfx(PrivateKey);
            pfxBuilder.FullChain = fullChain;

            if (issuers != null)
            {
                pfxBuilder.AddIssuers(issuers);
            }
            
            return pfxBuilder.Build(friendlyName, password);
        }
    }
}

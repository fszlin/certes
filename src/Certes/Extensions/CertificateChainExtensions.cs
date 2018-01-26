using System.Text;
using Certes.Acme;
using Certes.Pkcs;

namespace Certes
{
    /// <summary>
    /// Extension methods for <see cref="CertificateChain"/>.
    /// </summary>
    public static class CertificateChainExtensions
    {
        /// <summary>
        /// Converts the certificate to PFX with the key.
        /// </summary>
        /// <param name="certificateChain">The certificate chain.</param>
        /// <param name="certKey">The certificate private key.</param>
        /// <returns>The PFX.</returns>
        public static PfxBuilder ToPfx(this CertificateChain certificateChain, IKey certKey)
        {
            var pfx = new PfxBuilder(Encoding.UTF8.GetBytes(certificateChain.Certificate), certKey);
            if (certificateChain.Issuers != null)
            {
                foreach (var issuer in certificateChain.Issuers)
                {
                    pfx.AddIssuer(Encoding.UTF8.GetBytes(issuer));
                }
            }

            return pfx;
        }
    }
}

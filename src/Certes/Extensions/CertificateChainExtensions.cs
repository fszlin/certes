using System.IO;
using Certes.Acme;
using Certes.Pkcs;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.X509;

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
            var pfx = new PfxBuilder(certificateChain.Certificate.ToDer(), certKey);
            if (certificateChain.Issuers != null)
            {
                foreach (var issuer in certificateChain.Issuers)
                {
                    pfx.AddIssuer(issuer.ToDer());
                }
            }

            return pfx;
        }

        /// <summary>
        /// Encodes the full certificate chain in PEM.
        /// </summary>
        /// <param name="certificateChain">The certificate chain.</param>
        /// <param name="certKey">The certificate key.</param>
        /// <returns>The encoded certificate chain.</returns>
        /// <remarks>
        /// When a key is supplied, verifies that it matches the leaf certificate. This does not
        /// validate the certificate chain or establish trust in it.
        /// </remarks>
        public static string ToPem(this CertificateChain certificateChain, IKey certKey = null)
        {
            if (certKey != null)
            {
                var certParser = new X509CertificateParser();
                var certificate = certParser.ReadCertificate(certificateChain.Certificate.ToDer());
                certKey.EnsureKeyMatches(certificate);
            }

            var certStore = new CertificateStore();
            foreach (var issuer in certificateChain.Issuers)
            {
                certStore.Add(issuer.ToDer());
            }

            var issuers = certStore.GetIssuers(certificateChain.Certificate.ToDer());

            using (var writer = new StringWriter())
            {
                if (certKey != null)
                {
                    writer.WriteLine(certKey.ToPem().TrimEnd());
                }

                writer.WriteLine(certificateChain.Certificate.ToPem().TrimEnd());

                var certParser = new X509CertificateParser();
                var pemWriter = new PemWriter(writer);
                foreach (var issuer in issuers)
                {
                    var cert = certParser.ReadCertificate(issuer);
                    pemWriter.WriteObject(cert);
                }

                return writer.ToString();
            }
        }
    }
}

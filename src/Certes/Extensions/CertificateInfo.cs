using System.Text;
using Certes.Pkcs;
using Org.BouncyCastle.X509;

namespace Certes
{
    /// <summary>
    /// 
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
        public ISignatureKey PrivateKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateInfo" /> class.
        /// </summary>
        /// <param name="pem">The pem.</param>
        /// <param name="privateKey">The private key.</param>
        public CertificateInfo(string pem, ISignatureKey privateKey)
        {
            this.pem = pem;
            PrivateKey = privateKey;
        }

        /// <summary>
        /// PFXs this instance.
        /// </summary>
        /// <returns></returns>
        public byte[] ToPfx(string friendlyName, string password, bool fullChain = true)
        {
            var pfxBuilder = new PfxBuilder(Encoding.UTF8.GetBytes(pem), PrivateKey);
            pfxBuilder.FullChain = fullChain;
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

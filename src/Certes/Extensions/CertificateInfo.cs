using Certes.Crypto;

namespace Certes
{
    /// <summary>
    /// 
    /// </summary>
    public class CertificateInfo
    {
        /// <summary>
        /// Gets the certificate in PEM.
        /// </summary>
        /// <value>
        /// The certificate in PEM.
        /// </value>
        public string Pem { get; }

        /// <summary>
        /// Gets the certificate key.
        /// </summary>
        /// <value>
        /// The certificate key.
        /// </value>
        public ISignatureKey CertificateKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateInfo" /> class.
        /// </summary>
        /// <param name="pem">The pem.</param>
        /// <param name="privateKey">The private key.</param>
        public CertificateInfo(string pem, ISignatureKey privateKey)
        {
            Pem = pem;
            CertificateKey = privateKey;
        }
    }

}

using System.Text;
using Org.BouncyCastle.X509;

namespace Certes.Acme
{
    internal class CertificateContent : IEncodable
    {
        private readonly string pem;

        public CertificateContent(string pem)
        {
            this.pem = pem.Trim();
        }

        public byte[] ToDer()
        {
            var certParser = new X509CertificateParser();
            var cert = certParser.ReadCertificate(
                Encoding.UTF8.GetBytes(pem));
            return cert.GetEncoded();
        }

        public string ToPem() => pem;
    }
}

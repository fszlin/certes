using Certes.Pkcs;
using System;

namespace Certes.Acme
{
    public class AcmeCertificate : KeyedAcmeResult<string>
    {
        public bool Revoked { get; set; }

        public AcmeCertificate Issuer { get; set; }
    }

    public static class AcmeCertificateExtensions
    {
        public static PfxBuilder ToPfx(this AcmeCertificate cert)
        {
            if (cert?.Raw == null)
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

namespace Certes.Acme
{
    public class AcmeCertificate : KeyedAcmeResult<string>
    {
        public bool Revoked { get; set; }

        public AcmeCertificate Issuer { get; set; }
    }

}

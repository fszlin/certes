using Newtonsoft.Json;

namespace Certes.Acme.Resource
{
    internal class CertificateRevocation
    {
        [JsonProperty("certificate")]
        public string Certificate { get; set; }

        [JsonProperty("reason")]
        public RevocationReason? Reason { get; set; }
    }
}

using Newtonsoft.Json;

namespace Certes.Jws
{
    internal class JwsPayload
    {
        [JsonProperty("protected")]
        public string Protected { get; set; }

        [JsonProperty("payload")]
        public string Payload { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }
    }

}

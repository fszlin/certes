using Newtonsoft.Json;

namespace Certes.Jws
{
    internal class JwsPayload
    {
        [JsonProperty("header")]
        public JwsUnprotectedHeader Header { get; set; }

        [JsonProperty("protected")]
        public string Protected { get; set; }

        [JsonProperty("payload")]
        public string Payload { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }
    }

}

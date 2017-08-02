using Certes.Json;
using Certes.Pkcs;
using Newtonsoft.Json;
using System.Text;

namespace Certes.Jws
{
    /// <summary>
    /// Represents an signer for JSON Web Signature.
    /// </summary>
    internal class JwsSigner
    {
        private readonly IAccountKey keyPair;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwsSigner"/> class.
        /// </summary>
        /// <param name="keyPair">The keyPair.</param>
        public JwsSigner(IAccountKey keyPair)
        {
            this.keyPair = keyPair;
        }

        /// <summary>
        /// Encodes this instance.
        /// </summary>
        public JwsPayload Sign(object payload, string nonce = null)
        {
            var jsonSettings = JsonUtil.CreateSettings();
            var unprotectedHeader = new JwsUnprotectedHeader
            {
                Algorithm = keyPair.Algorithm.ToJwsAlgorithm(),
                JsonWebKey = keyPair.JsonWebKey
            };

            var protectedHeader = new
            {
                nonce = nonce
            };

            var entityJson = JsonConvert.SerializeObject(payload, Formatting.None, jsonSettings);
            var protectedHeaderJson = JsonConvert.SerializeObject(protectedHeader, Formatting.None, jsonSettings);

            var payloadEncoded = JwsConvert.ToBase64String(Encoding.UTF8.GetBytes(entityJson));
            var protectedHeaderEncoded = JwsConvert.ToBase64String(Encoding.UTF8.GetBytes(protectedHeaderJson));

            var signature = $"{protectedHeaderEncoded}.{payloadEncoded}";
            var signatureBytes = Encoding.ASCII.GetBytes(signature);
            var signedSignatureBytes = keyPair.SignData(signatureBytes);
            var signedSignatureEncoded = JwsConvert.ToBase64String(signedSignatureBytes);

            var body = new JwsPayload
            {
                Header = unprotectedHeader,
                Protected = protectedHeaderEncoded,
                Payload = payloadEncoded,
                Signature = signedSignatureEncoded
            };

            return body;
        }
    }
}

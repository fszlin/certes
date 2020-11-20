using System;
using System.Text;
using Certes.Json;
using Newtonsoft.Json;

namespace Certes.Jws
{
    /// <summary>
    /// Represents an signer for JSON Web Signature.
    /// </summary>
    internal class JwsSigner
    {
        private readonly IKey keyPair;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwsSigner"/> class.
        /// </summary>
        /// <param name="keyPair">The keyPair.</param>
        public JwsSigner(IKey keyPair)
        {
            this.keyPair = keyPair;
        }

        /// <summary>
        /// Signs the specified payload.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <param name="nonce">The nonce.</param>
        /// <returns>The signed payload.</returns>
        public JwsPayload Sign(object payload, string nonce)
            => Sign(payload, null, null, nonce);

        /// <summary>
        /// Encodes this instance.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <param name="keyId">The key identifier.</param>
        /// <param name="url">The URL.</param>
        /// <param name="nonce">The nonce.</param>
        /// <returns>The signed payload.</returns>
        public JwsPayload Sign(
            object payload,
            Uri keyId = null,
            Uri url = null,
            string nonce = null)
        {
            var jsonSettings = JsonUtil.CreateSettings();
            var protectedHeader = (keyId) == null ?
                (object)new
                {
                    alg = keyPair.Algorithm.ToJwsAlgorithm(),
                    jwk = keyPair.JsonWebKey,
                    nonce,
                    url,
                } :
                new
                {
                    alg = keyPair.Algorithm.ToJwsAlgorithm(),
                    kid = keyId,
                    nonce,
                    url,
                };

            var entityJson = payload == null ?
                "" :
                JsonConvert.SerializeObject(payload, Formatting.None, jsonSettings);
            var protectedHeaderJson = JsonConvert.SerializeObject(protectedHeader, Formatting.None, jsonSettings);

            var payloadEncoded = JwsConvert.ToBase64String(Encoding.UTF8.GetBytes(entityJson));
            var protectedHeaderEncoded = JwsConvert.ToBase64String(Encoding.UTF8.GetBytes(protectedHeaderJson));

            var signature = $"{protectedHeaderEncoded}.{payloadEncoded}";
            var signatureBytes = Encoding.UTF8.GetBytes(signature);
            var signedSignatureBytes = keyPair.GetSigner().SignData(signatureBytes);
            var signedSignatureEncoded = JwsConvert.ToBase64String(signedSignatureBytes);

            var body = new JwsPayload
            {
                Protected = protectedHeaderEncoded,
                Payload = payloadEncoded,
                Signature = signedSignatureEncoded
            };

            return body;
        }
    }
}

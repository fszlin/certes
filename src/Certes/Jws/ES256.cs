using System.IO;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Certes.Jws
{
    internal class ES256 : IJsonWebAlgorithm
    {
        private readonly AsymmetricCipherKeyPair keyPair;
        public JsonWebKey JsonWebKey
        {
            get
            {
                var parameters = (ECPublicKeyParameters)keyPair.Public;
                return new EcJsonWebKey
                {
                    KeyType = "EC",
                    Curve = "P-256",
                    X = JwsConvert.ToBase64String(parameters.Q.AffineXCoord.ToBigInteger().ToByteArrayUnsigned()),
                    Y = JwsConvert.ToBase64String(parameters.Q.AffineYCoord.ToBigInteger().ToByteArrayUnsigned()),
                };
            }
        }

        public ES256(AsymmetricCipherKeyPair keyPair)
        {
            this.keyPair = keyPair;
        }

        /// <summary>
        /// Computes the hash for given data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The hash.</returns>
        public byte[] ComputeHash(byte[] data) => DigestUtilities.CalculateDigest("SHA256", data);

        public byte[] SignData(byte[] data)
        {
            var signer = SignerUtilities.GetSigner("SHA-256withECDSA");
            signer.Init(true, keyPair.Private);
            signer.BlockUpdate(data, 0, data.Length);
            var signature = signer.GenerateSignature();

            var sequence = (Asn1Sequence)Asn1Object.FromByteArray(signature);
            using (var buffer = new MemoryStream())
            {
                foreach (Asn1Object obj in sequence)
                {
                    var derInt = (DerInteger)obj.ToAsn1Object();
                    var intBytes = derInt.Value.ToByteArrayUnsigned();
                    buffer.Write(intBytes, 0, intBytes.Length);
                }

                return buffer.ToArray();
            }
        }

        public void Save(Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                var pemWriter = new PemWriter(writer);
                pemWriter.WriteObject(keyPair);
            }
        }

        public static AsymmetricCipherKeyPair CreateKeyPair()
        {
            var generator = GeneratorUtilities.GetKeyPairGenerator("ECDSA");
            var generatorParams = new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256r1, new SecureRandom());
            generator.Init(generatorParams);
            var keyPair = generator.GenerateKeyPair();
            return keyPair;
        }
    }
}

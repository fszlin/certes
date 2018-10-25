using System;
using System.IO;
using System.Text;
using Certes.Acme.Resource;
using Certes.Crypto;
using Certes.Json;
using Certes.Jws;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Certes
{
    /// <summary>
    /// Represents key parameters used for signing.
    /// </summary>
    public interface IKey : IEncodable
    {
        /// <summary>
        /// Gets the algorithm.
        /// </summary>
        /// <value>
        /// The algorithm.
        /// </value>
        KeyAlgorithm Algorithm { get; }

        /// <summary>
        /// Gets the json web key.
        /// </summary>
        /// <value>
        /// The json web key.
        /// </value>
        JsonWebKey JsonWebKey { get; }
    }

    /// <summary>
    /// Helper methods for <see cref="AccountKey"/>.
    /// </summary>
    public static class ISignatureKeyExtensions
    {
        private static readonly DerObjectIdentifier acmeValidationV1Id = new DerObjectIdentifier("1.3.6.1.5.5.7.1.31");
        private static readonly KeyAlgorithmProvider signatureAlgorithmProvider = new KeyAlgorithmProvider();
        private static readonly JsonSerializerSettings thumbprintSettings = JsonUtil.CreateSettings();

        /// <summary>
        /// Generates the thumbprint for the given account <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The account key.</param>
        /// <returns>The thumbprint.</returns>
        internal static byte[] GenerateThumbprint(this IKey key)
        {
            var jwk = key.JsonWebKey;
            var json = JsonConvert.SerializeObject(jwk, Formatting.None, thumbprintSettings);
            var bytes = Encoding.UTF8.GetBytes(json);
            var hashed = DigestUtilities.CalculateDigest("SHA256", bytes);

            return hashed;
        }

        /// <summary>
        /// Generates the base64 encoded thumbprint for the given account <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The account key.</param>
        /// <returns>The thumbprint.</returns>
        public static string Thumbprint(this IKey key)
        {
            var jwkThumbprint = key.GenerateThumbprint();
            return JwsConvert.ToBase64String(jwkThumbprint);
        }

        /// <summary>
        /// Generates key authorization string.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="token">The challenge token.</param>
        /// <returns>The key authorization string.</returns>
        public static string KeyAuthorization(this IKey key, string token)
        {
            var jwkThumbprintEncoded = key.Thumbprint();
            return $"{token}.{jwkThumbprintEncoded}";
        }

        /// <summary>
        /// Generates the value for DNS TXT record.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="token">The challenge token.</param>
        /// <returns>The DNS text value for dns-01 validation.</returns>
        public static string DnsTxt(this IKey key, string token)
        {
            var keyAuthz = key.KeyAuthorization(token);
            var hashed = DigestUtilities.CalculateDigest("SHA256", Encoding.UTF8.GetBytes(keyAuthz));
            return JwsConvert.ToBase64String(hashed);
        }

        /// <summary>
        /// Generates the certificate for <see cref="ChallengeTypes.TlsAlpn01" /> validation.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="token">The <see cref="ChallengeTypes.TlsAlpn01" /> token.</param>
        /// <param name="subjectName">Name of the subject.</param>
        /// <param name="certificateKey">The certificate key pair.</param>
        /// <returns>The tls-alpn-01 certificate in PEM.</returns>
        public static string TlsAlpnCertificate(this IKey key, string token, string subjectName, IKey certificateKey)
        {
            var keyAuthz = key.KeyAuthorization(token);
            var hashed = DigestUtilities.CalculateDigest("SHA256", Encoding.UTF8.GetBytes(keyAuthz));

            var (_, keyPair) = signatureAlgorithmProvider.GetKeyPair(certificateKey.ToDer());

            var signatureFactory = new Asn1SignatureFactory(certificateKey.Algorithm.ToPkcsObjectId(), keyPair.Private, new SecureRandom());
            var gen = new X509V3CertificateGenerator();
            var certName = new X509Name($"CN={subjectName}");
            var serialNo = BigInteger.ProbablePrime(120, new SecureRandom());

            gen.SetSerialNumber(serialNo);
            gen.SetSubjectDN(certName);
            gen.SetIssuerDN(certName);
            gen.SetNotBefore(DateTime.UtcNow);
            gen.SetNotAfter(DateTime.UtcNow.AddDays(7));
            gen.SetPublicKey(keyPair.Public);

            // SAN for validation
            var gns = new[] { new GeneralName(GeneralName.DnsName, subjectName) };
            gen.AddExtension(X509Extensions.SubjectAlternativeName.Id, false, new GeneralNames(gns));

            // ACME-TLS/1
            gen.AddExtension(
                acmeValidationV1Id,
                true,
                hashed);

            var newCert = gen.Generate(signatureFactory);

            using (var sr = new StringWriter())
            {
                var pemWriter = new PemWriter(sr);
                pemWriter.WriteObject(newCert);
                return sr.ToString();
            }
        }
    }
}

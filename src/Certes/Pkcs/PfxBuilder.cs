using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Certes.Crypto;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

namespace Certes.Pkcs
{
    /// <summary>
    /// Supports generating PFX from the certificate and key pair.
    /// </summary>
    /// <remarks>
    /// This packages a certificate chain; it does not validate a certification path. Issuers
    /// are accepted only after verifying that they signed the certificate below them, but
    /// issuer constraints, revocation, and complete-path validity are not checked. The supplied
    /// private key is checked against the leaf certificate, but trust decisions belong to the
    /// relying party that consumes the PFX.
    /// </remarks>
    public class PfxBuilder
    {
        private static readonly KeyAlgorithmProvider signatureAlgorithmProvider = new KeyAlgorithmProvider();

        private readonly X509Certificate certificate;
        private readonly IKey privateKey;
        private readonly CertificateStore certificateStore = new CertificateStore();

        /// <summary>
        /// Gets or sets a value indicating whether to include the full certificate chain in the PFX.
        /// </summary>
        /// <value>
        ///   <c>true</c> if include the full certificate chain in the PFX; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// The chain is the certificate followed by the issuers linked to it, excluding any
        /// self-signed root. It is not a validated certification path.
        /// </remarks>
        public bool FullChain { get; set; } = true;

        /// <summary>
        /// Gets or sets the algorithms used to protect the private key and certificates.
        /// </summary>
        /// <value>
        /// Defaults to <see cref="PfxEncryption.Aes256"/>.
        /// </value>
        /// <remarks>
        /// The integrity check (MAC) uses HMAC-SHA1 in both modes; BouncyCastle does not
        /// expose another MAC algorithm. OpenSSL 3 and .NET accept it.
        /// </remarks>
        public PfxEncryption Encryption { get; set; } = PfxEncryption.Aes256;

        /// <summary>
        /// Initializes a new instance of the <see cref="PfxBuilder"/> class.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <param name="privateKeyInfo">The private key information.</param>
        public PfxBuilder(byte[] certificate, KeyInfo privateKeyInfo)
            : this(certificate, signatureAlgorithmProvider.GetKey(privateKeyInfo.PrivateKeyInfo))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PfxBuilder"/> class.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <param name="privateKey">The private key.</param>
        public PfxBuilder(byte[] certificate, IKey privateKey)
        {
            var certParser = new X509CertificateParser();
            this.certificate = certParser.ReadCertificate(certificate);
            this.privateKey = privateKey;
        }

        /// <summary>
        /// Adds an issuer certificate.
        /// </summary>
        /// <param name="certificate">The issuer certificate.</param>
        public void AddIssuer(byte[] certificate) => certificateStore.Add(certificate);

        /// <summary>
        /// Adds issuer certificates.
        /// </summary>
        /// <param name="certificates">The issuer certificates.</param>
        public void AddIssuers(byte[] certificates) => certificateStore.Add(certificates);

        /// <summary>
        /// Builds the PFX with specified friendly name.
        /// </summary>
        /// <param name="friendlyName">The friendly name.</param>
        /// <param name="password">The password.</param>
        /// <returns>The PFX data.</returns>
        /// <remarks>
        /// Verifies that the private key matches the leaf certificate, then packages the
        /// certificate and its linked issuers. No certification path validation is performed,
        /// so an expired or otherwise unusable certificate is exported as supplied.
        /// </remarks>
        public byte[] Build(string friendlyName, string password)
        {
            var keyPair = privateKey.GetKeyPairFor(certificate);
            var store = CreateStoreBuilder(Encryption).Build();

            var entry = new X509CertificateEntry(certificate);
            store.SetCertificateEntry(friendlyName, entry);

            if (FullChain && !certificate.IssuerDN.Equivalent(certificate.SubjectDN))
            {
                var certChain = FindIssuers();
                var certChainEntries = certChain.Select(c => new X509CertificateEntry(c)).ToList();
                certChainEntries.Add(entry);

                store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(keyPair.Private), certChainEntries.ToArray());
            }
            else
            {
                store.SetKeyEntry(friendlyName, new AsymmetricKeyEntry(keyPair.Private), new[] { entry });
            }

            using (var buffer = new MemoryStream())
            {
                store.Save(buffer, password.ToCharArray(), new SecureRandom());
                return buffer.ToArray();
            }
        }

        private static Pkcs12StoreBuilder CreateStoreBuilder(PfxEncryption encryption)
        {
            switch (encryption)
            {
                case PfxEncryption.Aes256:
                    return new Pkcs12StoreBuilder()
                        .SetKeyAlgorithm(NistObjectIdentifiers.IdAes256Cbc, PkcsObjectIdentifiers.IdHmacWithSha256)
                        .SetCertAlgorithm(NistObjectIdentifiers.IdAes256Cbc, PkcsObjectIdentifiers.IdHmacWithSha256);
                case PfxEncryption.Legacy:
                    return new Pkcs12StoreBuilder()
                        .SetKeyAlgorithm(PkcsObjectIdentifiers.PbeWithShaAnd3KeyTripleDesCbc)
                        .SetCertAlgorithm(PkcsObjectIdentifiers.PbewithShaAnd40BitRC2Cbc);
                default:
                    throw new ArgumentOutOfRangeException(nameof(Encryption), encryption, null);
            }
        }

        /// <summary>
        /// Builds the chain packaged with the key entry: the certificate followed by the
        /// issuers linked to it, excluding any self-signed root.
        /// </summary>
        /// <remarks>
        /// This packages a chain; it does not validate a certification path. Issuers are linked
        /// by <see cref="CertificateStore"/>, which verifies that each one signed the
        /// certificate below it, but issuer constraints, revocation and complete-path validity
        /// are not evaluated. The self-signed root is excluded because relying parties supply
        /// their own trust anchors, and ACME servers are not expected to return one
        /// (RFC 8555, section 7.4.2).
        /// </remarks>
        private IList<X509Certificate> FindIssuers()
        {
            var certParser = new X509CertificateParser();
            var issuers = certificateStore
                .GetIssuers(certificate.GetEncoded())
                .Select(der => certParser.ReadCertificate(der))
                .Where(cert => !cert.IssuerDN.Equivalent(cert.SubjectDN));

            var chain = new List<X509Certificate> { certificate };
            chain.AddRange(issuers);
            return chain;
        }
    }
}

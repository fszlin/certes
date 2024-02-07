using System.Collections.Generic;
using System.IO;
using System.Linq;
using Certes.Crypto;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;

namespace Certes.Pkcs
{
    /// <summary>
    /// Supports generating PFX from the certificate and key pair.
    /// </summary>
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
        public bool FullChain { get; set; } = true;

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
        public byte[] Build(string friendlyName, string password)
        {
            var keyPair = LoadKeyPair();
            var store = new Pkcs12StoreBuilder().Build();

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

        private IList<X509Certificate> FindIssuers()
        {
            var certParser = new X509CertificateParser();
            var certificates = certificateStore
                .GetIssuers(certificate.GetEncoded())
                .Select(der => certParser.ReadCertificate(der))
                .Select(cert => new
                {
                    IsRoot = cert.IssuerDN.Equivalent(cert.SubjectDN),
                    Cert = cert
                });

            var rootCerts = new HashSet<TrustAnchor>(certificates.Where(c => c.IsRoot).Select(c => new TrustAnchor(c.Cert, null)));
            var intermediateCerts = certificates.Where(c => !c.IsRoot).Select(c => c.Cert).ToList();
            intermediateCerts.Add(certificate);

            var target = new X509CertStoreSelector
            {
                Certificate = certificate
            };

            var builderParams = new PkixBuilderParameters(rootCerts, target)
            {
                IsRevocationEnabled = false
            };

            var x509CertStore = CollectionUtilities.CreateStore(intermediateCerts);
            builderParams.AddStoreCert(x509CertStore);

            var builder = new PkixCertPathBuilder();
            var result = builder.Build(builderParams);

            var fullChain = result.CertPath.Certificates.Cast<X509Certificate>().ToArray();
            return fullChain;
        }

        private AsymmetricCipherKeyPair LoadKeyPair()
        {
            var (_, keyPair) = signatureAlgorithmProvider.GetKeyPair(privateKey.ToDer());
            return keyPair;
        }
    }
}

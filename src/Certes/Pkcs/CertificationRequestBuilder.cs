using System;
using System.Collections.Generic;
using System.Linq;
using Certes.Crypto;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Pkcs;

namespace Certes.Pkcs
{
    /// <summary>
    /// Represents a CSR builder.
    /// </summary>
    /// <seealso cref="Certes.Pkcs.ICertificationRequestBuilder" />
    public class CertificationRequestBuilder : ICertificationRequestBuilder
    {
        private static readonly KeyAlgorithmProvider keyAlgorithmProvider = new KeyAlgorithmProvider();
        private string commonName;
        private readonly List<(DerObjectIdentifier Id, string Value)> attributes = new List<(DerObjectIdentifier, string)>();
        private IList<string> subjectAlternativeNames = new List<string>();

        private string pkcsObjectId;
        private AsymmetricCipherKeyPair keyPair;

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public IKey Key { get; }

        /// <summary>
        /// Gets the subject alternative names.
        /// </summary>
        /// <value>
        /// The subject alternative names.
        /// </value>
        public IList<string> SubjectAlternativeNames
        {
            get
            {
                return subjectAlternativeNames;
            }
            set
            {
                this.subjectAlternativeNames = value ??
                    throw new ArgumentNullException(nameof(SubjectAlternativeNames));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificationRequestBuilder"/> class.
        /// </summary>
        public CertificationRequestBuilder()
            : this(KeyFactory.NewKey(KeyAlgorithm.RS256))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificationRequestBuilder"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        public CertificationRequestBuilder(IKey key)
        {
            Key = key;
        }
        /// <summary>
        /// Adds the distinguished name as certificate subject.
        /// </summary>
        /// <param name="distinguishedName">The distinguished name.</param>
        public void AddName(string distinguishedName)
        {
            X509Name name;
            try
            {
                name = new X509Name(distinguishedName);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentOutOfRangeException(
                    $"{distinguishedName} contains an ivalid X509 name.", ex);
            }

            var oidList = name.GetOidList();
            var valueList = name.GetValueList();
            var len = oidList.Count;
            for (var i = 0; i < len; ++i)
            {
                var id = (DerObjectIdentifier)oidList[i];
                var value = valueList[i].ToString();
                attributes.Add((id, value));

                if (id == X509Name.CN)
                {
                    this.commonName = value;
                }
            }
        }

        /// <summary>
        /// Adds the name.
        /// </summary>
        /// <param name="keyOrCommonName">Name of the key or common.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// If <paramref name="keyOrCommonName"/> is not a valid X509 name.
        /// </exception>
        public void AddName(string keyOrCommonName, string value)
            => AddName($"{keyOrCommonName}={value}");

        /// <summary>
        /// Generates the CSR.
        /// </summary>
        /// <returns>
        /// The CSR data.
        /// </returns>
        public byte[] Generate()
        {
            var csr = GeneratePkcs10();
            return csr.GetDerEncoded();
        }

        private Pkcs10CertificationRequest GeneratePkcs10()
        {
            var x509 = new X509Name(attributes.Select(p => p.Id).ToArray(), attributes.Select(p => p.Value).ToArray());

            if (this.SubjectAlternativeNames.Count == 0)
            {
                this.SubjectAlternativeNames.Add(commonName);
            }

            var altNames = this.SubjectAlternativeNames
                .Distinct()
                .Select(n => new GeneralName(GeneralName.DnsName, n))
                .ToArray();

            var extensions = new X509Extensions(new Dictionary<DerObjectIdentifier, X509Extension>
            {
                { X509Extensions.BasicConstraints, new X509Extension(false, new DerOctetString(new BasicConstraints(false))) },
                { X509Extensions.KeyUsage, new X509Extension(false, new DerOctetString(new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment | KeyUsage.NonRepudiation))) },
                { X509Extensions.SubjectAlternativeName, new X509Extension(false, new DerOctetString(new GeneralNames(altNames))) }
            });

            var attribute = new AttributePkcs(PkcsObjectIdentifiers.Pkcs9AtExtensionRequest, new DerSet(extensions));

            LoadKeyPair();
            var signatureFactory = new Asn1SignatureFactory(pkcsObjectId, keyPair.Private);
            return new Pkcs10CertificationRequest(signatureFactory, x509, keyPair.Public, new DerSet(attribute));
        }

        private void LoadKeyPair()
        {
            var (algo, keyPair) = keyAlgorithmProvider.GetKeyPair(Key.ToDer());
            pkcsObjectId = algo.ToPkcsObjectId();
            this.keyPair = keyPair;
        }
    }
}

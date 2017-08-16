using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Pkcs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Certes.Pkcs
{
    /// <summary>
    /// Supports building Certificate Signing Request (CSR).
    /// </summary>
    /// <seealso cref="Certes.Pkcs.ICertificationRequestBuilder" />
    public abstract class CertificationRequestBuilderBase : ICertificationRequestBuilder
    {
        private string commonName;
        private readonly List<(DerObjectIdentifier Id, string Value)> attributes = new List<(DerObjectIdentifier, string)>();

        /// <summary>
        /// Gets the algorithm.
        /// </summary>
        /// <value>
        /// The algorithm.
        /// </value>
        protected abstract SignatureAlgorithm Algorithm { get; }

        /// <summary>
        /// Gets the key pair.
        /// </summary>
        /// <value>
        /// The key pair.
        /// </value>
        protected abstract AsymmetricCipherKeyPair KeyPair { get; }

        /// <summary>
        /// Gets the subject alternative names.
        /// </summary>
        /// <value>
        /// The subject alternative names.
        /// </value>
        public IList<string> SubjectAlternativeNames { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificationRequestBuilderBase"/> class.
        /// </summary>
        public CertificationRequestBuilderBase()
        {
			SubjectAlternativeNames = new List<string>();
        }

        /// <summary>
        /// Adds the distinguished name as certificate subject.
        /// </summary>
        /// <param name="distinguishedName">The distinguished name.</param>
        public void AddName(string distinguishedName)
        {
            var name = new X509Name(distinguishedName);

            var oidList = name.GetOidList();
            var valueList = name.GetValueList();
            var len = oidList.Count;
            for (var i = 0; i < len; ++i)
            {
                var id = (DerObjectIdentifier)oidList[i];
                var value = valueList[i]?.ToString();
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
        /// <exception cref="System.ArgumentException"></exception>
        public void AddName(string keyOrCommonName, string value)
        {
            DerObjectIdentifier id;
            var lowered = keyOrCommonName.ToLowerInvariant();
            if (X509Name.DefaultLookup.Contains(lowered))
            {
                id = (DerObjectIdentifier)X509Name.DefaultLookup[lowered];
                this.attributes.Add((id, value));

                if (id == X509Name.CN)
                {
                    this.commonName = value;
                }
            }
            else
            {
                throw new ArgumentException();
            }
        }

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

        /// <summary>
        /// Exports the key used to generate the CSR.
        /// </summary>
        /// <returns>
        /// The key data.
        /// </returns>
        public KeyInfo Export()
        {
            return this.KeyPair.Export();
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
            
            var signatureFactory = new Asn1SignatureFactory(this.Algorithm.ToPkcsObjectId(), this.KeyPair.Private);
            var csr = new Pkcs10CertificationRequest(signatureFactory, x509, KeyPair.Public, new DerSet(attribute), KeyPair.Private);

            var valid = csr.Verify();
            if (!valid)
            {
                throw new Exception();
            }

            return csr;
        }
    }
}

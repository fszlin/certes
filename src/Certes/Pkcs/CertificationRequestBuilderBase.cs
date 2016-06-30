using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Certes.Pkcs
{
    public abstract class CertificationRequestBuilderBase : ICertificationRequestBuilder
    {
        private string commonName;
        private List<Tuple<DerObjectIdentifier, string>> attributes = new List<Tuple<DerObjectIdentifier, string>>();

        protected abstract SignatureAlgorithm Algorithm { get; }
        protected abstract AsymmetricCipherKeyPair KeyPair { get; }

        public List<string> SubjectAlternativeNames { get; } = new List<string>();

        public CertificationRequestBuilderBase()
        {
        }

        public void AddName(string distinguishednName)
        {
            var name = new X509Name(distinguishednName);

            var oidList = name.GetOidList();
            var valueList = name.GetValueList();
            var len = oidList.Count;
            for (var i = 0; i < len; ++i)
            {
                var id = (DerObjectIdentifier)oidList[i];
                var value = valueList[i]?.ToString();
                attributes.Add(Tuple.Create(id, value));

                if (id == X509Name.CN)
                {
                    this.commonName = value;
                }
            }
        }

        public void AddName(string keyOrCommonName, string value)
        {
            DerObjectIdentifier id;
            if (X509Name.DefaultLookup.Contains(keyOrCommonName))
            {
                id = (DerObjectIdentifier)X509Name.DefaultLookup[keyOrCommonName.ToLowerInvariant()];
                this.attributes.Add(Tuple.Create(id, value));

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

        public byte[] Generate()
        {
            var csr = GeneratePkcs10();
            return csr.GetDerEncoded();
        }
        
        public KeyInfo Export()
        {
            return this.KeyPair.Export();
        }

        private Pkcs10CertificationRequest GeneratePkcs10()
        {
            var x509 = new X509Name(attributes.Select(p => p.Item1).ToArray(), attributes.Select(p => p.Item2).ToArray());

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

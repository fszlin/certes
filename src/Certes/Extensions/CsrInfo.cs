using System.Collections.Generic;
using System.Linq;
using Certes.Crypto;

namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    public class CertificateInfo
    {
        /// <summary>
        /// Gets the certificate in PEM.
        /// </summary>
        /// <value>
        /// The certificate in PEM.
        /// </value>
        public string Pem { get; }

        /// <summary>
        /// Gets the certificate key.
        /// </summary>
        /// <value>
        /// The certificate key.
        /// </value>
        public ISignatureKey CertificateKey { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificateInfo" /> class.
        /// </summary>
        /// <param name="pem">The pem.</param>
        /// <param name="privateKey">The private key.</param>
        public CertificateInfo(string pem, ISignatureKey privateKey)
        {
            Pem = pem;
            CertificateKey = privateKey;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class CsrInfo
    {
        private readonly Dictionary<string, string> data = new Dictionary<string, string>();

        /// <summary>
        /// Gets or sets the two-letter ISO abbreviation for your country.
        /// </summary>
        /// <value>
        /// The two-letter ISO abbreviation for your country.
        /// </value>
        public string CountryName
        {
            get => data.TryGetValue("C", out var value) ? value : null;
            set => data["C"] = value;
        }

        /// <summary>
        /// Gets or sets the state or province where your organization is located. Can not be abbreviated.
        /// </summary>
        /// <value>
        /// The state or province where your organization is located. Can not be abbreviated.
        /// </value>
        public string State
        {
            get => data.TryGetValue("ST", out var value) ? value : null;
            set => data["ST"] = value;
        }

        /// <summary>
        /// Gets or sets the city where your organization is located.
        /// </summary>
        /// <value>
        /// The city where your organization is located.
        /// </value>
        public string Locality
        {
            get => data.TryGetValue("L", out var value) ? value : null;
            set => data["L"] = value;
        }

        /// <summary>
        /// Gets or sets the exact legal name of your organization. Do not abbreviate.
        /// </summary>
        /// <value>
        /// The exact legal name of your organization. Do not abbreviate.
        /// </value>
        public string Organization
        {
            get => data.TryGetValue("O", out var value) ? value : null;
            set => data["O"] = value;
        }

        /// <summary>
        /// Gets or sets the optional organizational information.
        /// </summary>
        /// <value>
        /// The optional organizational information.
        /// </value>
        public string OrganizationUnit
        {
            get => data.TryGetValue("OU", out var value) ? value : null;
            set => data["OU"] = value;
        }

        /// <summary>
        /// Gets or sets the common name for the CSR.
        /// If not set, the first identifier of the ACME order will be chosen as common name.
        /// </summary>
        /// <value>
        /// The common name for the CSR.
        /// </value>
        public string CommonName
        {
            get => data.TryGetValue("CN", out var value) ? value : null;
            set => data["CN"] = value;
        }

        internal IEnumerable<(string name, string value)> Fields
        {
            get => data.Select(p => (p.Key, p.Value));
        }
    }
}

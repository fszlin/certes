using System;
using Certes.Acme;
using Certes.Jws;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;

namespace Certes
{
    /// <summary>
    /// Helper methods for ACME Renewal Information (ARI), as defined in RFC 9773.
    /// </summary>
    public static class RenewalInfoExtensions
    {
        /// <summary>
        /// Gets the ARI certificate identifier of the end-entity certificate in the chain.
        /// </summary>
        /// <param name="certificateChain">The certificate chain.</param>
        /// <returns>
        /// The certificate identifier, for use with
        /// <see cref="IAcmeContextExtensions.GetRenewalInfo(IAcmeContext, string)"/> and
        /// <see cref="IAcmeContextExtensions.NewReplacementOrder(IAcmeContext, System.Collections.Generic.IList{string}, string, System.DateTimeOffset?, System.DateTimeOffset?)"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="certificateChain"/> is <c>null</c>.</exception>
        /// <exception cref="AcmeException">If the certificate has no authority key identifier.</exception>
        public static string GetRenewalInfoCertificateId(this CertificateChain certificateChain)
        {
            if (certificateChain == null)
            {
                throw new ArgumentNullException(nameof(certificateChain));
            }

            return certificateChain.Certificate.GetRenewalInfoCertificateId();
        }

        /// <summary>
        /// Gets the ARI certificate identifier of the certificate.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns>
        /// The certificate identifier, for use with
        /// <see cref="IAcmeContextExtensions.GetRenewalInfo(IAcmeContext, string)"/> and
        /// <see cref="IAcmeContextExtensions.NewReplacementOrder(IAcmeContext, System.Collections.Generic.IList{string}, string, System.DateTimeOffset?, System.DateTimeOffset?)"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">If <paramref name="certificate"/> is <c>null</c>.</exception>
        /// <exception cref="AcmeException">If the certificate has no authority key identifier.</exception>
        public static string GetRenewalInfoCertificateId(this IEncodable certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate));
            }

            return GetCertificateId(certificate.ToDer());
        }

        internal static string GetCertificateId(byte[] certificateDer)
        {
            var cert = new X509CertificateParser().ReadCertificate(certificateDer);

            // RFC 9773 section 4.1: base64url(AKI keyIdentifier) "." base64url(DER serial number content octets)
            var akiValue = cert.GetExtensionValue(X509Extensions.AuthorityKeyIdentifier);
            var keyIdentifier = akiValue == null
                ? null
                : AuthorityKeyIdentifier.GetInstance(X509ExtensionUtilities.FromExtensionValue(akiValue))
                    .KeyIdentifier?.GetOctets();

            if (keyIdentifier == null || keyIdentifier.Length == 0)
            {
                throw new AcmeException("The certificate does not contain an authority key identifier.");
            }

            // BigInteger.ToByteArray returns the minimal two's complement big-endian encoding,
            // which matches the content octets of the DER INTEGER.
            var serial = cert.SerialNumber.ToByteArray();

            return $"{JwsConvert.ToBase64String(keyIdentifier)}.{JwsConvert.ToBase64String(serial)}";
        }
    }
}

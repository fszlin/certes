using System;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Pkcs;

namespace Certes
{
    /// <summary>
    /// Represents a ACME client.
    /// </summary>
    public interface IAcmeClient
    {
        /// <summary>
        /// Gets the HTTP handler.
        /// </summary>
        /// <value>
        /// The HTTP handler.
        /// </value>
        IAcmeHttpHandler HttpHandler { get; }

        /// <summary>
        /// Submits the challenge for the ACME server to validate.
        /// </summary>
        /// <param name="authChallenge">The authentication challenge.</param>
        /// <returns>The challenge updated.</returns>
        Task<AcmeResult<Challenge>> CompleteChallenge(Challenge authChallenge);

        /// <summary>
        /// Computes the DNS value for the <paramref name="challenge"/>.
        /// </summary>
        /// <param name="challenge">The challenge.</param>
        /// <returns>The value for the text DNS record.</returns>
        string ComputeDnsValue(Challenge challenge);

        /// <summary>
        /// Computes the key authorization string for <paramref name="challenge"/>.
        /// </summary>
        /// <param name="challenge">The challenge.</param>
        /// <returns>The key authorization string.</returns>
        string ComputeKeyAuthorization(Challenge challenge);

        /// <summary>
        /// Gets the authorization from <paramref name="location"/>.
        /// </summary>
        /// <param name="location">The authorization location URI.</param>
        /// <returns>The authorization retrieved.</returns>
        Task<AcmeResult<Authorization>> GetAuthorization(Uri location);

        /// <summary>
        /// Create a new authorization.
        /// </summary>
        /// <param name="identifier">The identifier to be authorized.</param>
        /// <returns>The authorization created.</returns>
        Task<AcmeResult<Authorization>> NewAuthorization(AuthorizationIdentifier identifier);

        /// <summary>
        /// Creates a new certificate.
        /// </summary>
        /// <param name="csrProvider">The certificate signing request (CSR) provider.</param>
        /// <returns>The certificate issued.</returns>
        Task<AcmeCertificate> NewCertificate(ICertificationRequestBuilder csrProvider);

        /// <summary>
        /// Creates a new certificate.
        /// </summary>
        /// <param name="csrBytes">The certificate signing request data.</param>
        /// <returns>The certificate issued.</returns>
        Task<AcmeCertificate> NewCertificate(byte[] csrBytes);

        /// <summary>
        /// Creates a new registraton.
        /// </summary>
        /// <param name="contact">The contact method, e.g. <c>mailto:admin@example.com</c>.</param>
        /// <returns>The ACME account created.</returns>
        Task<AcmeAccount> NewRegistraton(params string[] contact);

        /// <summary>
        /// Revokes the certificate.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <returns>The certificate revoked.</returns>
        Task<AcmeCertificate> RevokeCertificate(AcmeCertificate certificate);

        /// <summary>
        /// Updates the registration.
        /// </summary>
        /// <param name="account">The account to update.</param>
        /// <returns>The updated ACME account.</returns>
        Task<AcmeAccount> UpdateRegistration(AcmeAccount account);

        /// <summary>
        /// Deletes the registration.
        /// </summary>
        /// <returns>The awaitable.</returns>
        Task DeleteRegistration(AcmeAccount account);

        /// <summary>
        /// Uses the specified account key data.
        /// </summary>
        /// <param name="keyData">The account key data.</param>
        void Use(KeyInfo keyData);
    }
}
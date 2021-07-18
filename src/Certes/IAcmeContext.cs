using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Jws;

namespace Certes
{
    /// <summary>
    /// Represents the context for ACME operations.
    /// </summary>
    public interface IAcmeContext
    {

        /// <summary>
        /// Gets the number of retries on a badNonce error.
        /// </summary>
        /// <value>
        /// The number of retries.
        /// </value>
        int BadNonceRetryCount { get; }

        /// <summary>
        /// Gets the directory URI.
        /// </summary>
        /// <value>
        /// The directory URI.
        /// </value>
        Uri DirectoryUri { get; }

        /// <summary>
        /// Gets the ACME HTTP client.
        /// </summary>
        /// <value>
        /// The ACME HTTP client.
        /// </value>
        IAcmeHttpClient HttpClient { get; }

        /// <summary>
        /// Gets the account key.
        /// </summary>
        /// <value>
        /// The account key.
        /// </value>
        IKey AccountKey { get; }

        /// <summary>
        /// Gets the ACME account context.
        /// </summary>
        /// <returns>The ACME account context.</returns>
        Task<IAccountContext> Account();

        /// <summary>
        /// Gets the ACME directory.
        /// </summary>
        /// <returns>
        /// The ACME directory.
        /// </returns>
        Task<Directory> GetDirectory();

        /// <summary>
        /// Creates an account.
        /// </summary>
        /// <param name="contact">The contact.</param>
        /// <param name="termsOfServiceAgreed">Set to <c>true</c> to accept the terms of service.</param>
        /// <param name="eabKeyId">Optional key identifier, if using external account binding.</param>
        /// <param name="eabKey">Optional EAB key, if using external account binding.</param>
        /// <param name="eabKeyAlg">Optional EAB key algorithm, if using external account binding, defaults to HS256 if not specified</param>
        /// <returns>
        /// The account created.
        /// </returns>
        Task<IAccountContext> NewAccount(IList<string> contact, bool termsOfServiceAgreed = false, string eabKeyId = null, string eabKey = null, string eabKeyAlg = null);

        /// <summary>
        /// Revokes the certificate.
        /// </summary>
        /// <param name="certificate">The certificate in DER format.</param>
        /// <param name="reason">The reason for revocation.</param>
        /// <param name="certificatePrivateKey">The certificate's private key.</param>
        /// <returns>
        /// The awaitable.
        /// </returns>
        Task RevokeCertificate(byte[] certificate, RevocationReason reason = RevocationReason.Unspecified, IKey certificatePrivateKey = null);

        /// <summary>
        /// Changes the account key.
        /// </summary>
        /// <param name="key">The new account key.</param>
        /// <returns>The account resource.</returns>
        Task<Account> ChangeKey(IKey key = null);

        /// <summary>
        /// Creates a new the order.
        /// </summary>
        /// <param name="identifiers">The identifiers.</param>
        /// <param name="notBefore">Th value of not before field for the certificate.</param>
        /// <param name="notAfter">The value of not after field for the certificate.</param>
        /// <returns>
        /// The order context created.
        /// </returns>
        Task<IOrderContext> NewOrder(IList<string> identifiers, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null);

        /// <summary>
        /// Signs the data with account key.
        /// </summary>
        /// <param name="entity">The data to sign.</param>
        /// <param name="uri">The URI for the request.</param>
        /// <returns>The JWS payload.</returns>
        Task<JwsPayload> Sign(object entity, Uri uri);

        /// <summary>
        /// Gets the order by specified location.
        /// </summary>
        /// <param name="location">The order location.</param>
        /// <returns>The order context.</returns>
        IOrderContext Order(Uri location);

        /// <summary>
        /// Gets the authorization by specified location.
        /// </summary>
        /// <param name="location">The authorization location.</param>
        /// <returns>The authorization context.</returns>
        IAuthorizationContext Authorization(Uri location);
    }
}

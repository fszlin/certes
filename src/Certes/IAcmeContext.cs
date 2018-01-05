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
        IAccountKey AccountKey { get; }

        /// <summary>
        /// Gets the ACME account.
        /// </summary>
        /// <returns></returns>
        Task<IAccountContext> Account();

        /// <summary>
        /// Gets the ACME directory.
        /// </summary>
        /// <returns>
        /// The ACME directory.
        /// </returns>
        Task<Directory> GetDirectory();

        /// <summary>
        /// Creates the account.
        /// </summary>
        /// <returns>
        /// The account created.
        /// </returns>
        Task<Account> NewAccount(IList<string> contact, bool termsOfServiceAgreed = false);

        /// <summary>
        /// Revokes the certificate.
        /// </summary>
        /// <returns>
        /// The awaitable.
        /// </returns>
        Task RevokeCertificate(byte[] certificate, RevocationReason reason = RevocationReason.Unspecified, IAccountKey certificatePrivateKey = null);

        /// <summary>
        /// Changes the account key.
        /// </summary>
        /// <returns>
        /// The awaitable.
        /// </returns>
        Task ChangeKey(AccountKey key = null);

        /// <summary>
        /// Create a new the order.
        /// </summary>
        /// <param name="identifiers">The identifiers.</param>
        /// <param name="notBefore">The not before.</param>
        /// <param name="notAfter">The not after.</param>
        /// <returns>
        /// TODO
        /// </returns>
        Task<IOrderContext> NewOrder(IList<string> identifiers, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null);

        /// <summary>
        /// Signs the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        Task<JwsPayload> Sign(object entity, Uri uri);
    }
}

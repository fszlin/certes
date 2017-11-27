using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Jws;

namespace Certes
{
    /// <summary>
    /// Presents the context for ACME operations.
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
        /// Gets the account.
        /// </summary>
        /// <value>
        /// The account.
        /// </value>
        IAccountContext Account { get; }

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
        Task<Account> CreateAccount(IList<string> contact, bool termsOfServiceAgreed = false);

        /// <summary>
        /// Revokes the certificate.
        /// </summary>
        /// <returns>
        /// The awaitable.
        /// </returns>
        Task RevokeCertificate();

        /// <summary>
        /// Changes the account key.
        /// </summary>
        /// <returns>
        /// The awaitable.
        /// </returns>
        Task ChangeKey(AccountKey key = null);
    }
}

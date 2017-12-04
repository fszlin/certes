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
        /// Gets the account location.
        /// </summary>
        /// <returns></returns>
        Task<Uri> GetAccountLocation();

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

        /// <summary>
        /// Create a bew the order.
        /// </summary>
        /// <param name="identifiers">The identifiers.</param>
        /// <param name="notBefore">The not before.</param>
        /// <param name="notAfter">The not after.</param>
        /// <returns>
        /// TODO
        /// </returns>
        Task CreateOrder(IList<string> identifiers, DateTimeOffset? notBefore = null, DateTimeOffset? notAfter = null);

        /// <summary>
        /// Signs the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        Task<JwsPayload> Sign(object entity, Uri uri);
    }

    internal static class IAcmeContextExtensions
    {
        internal static async Task<Uri> GetResourceUri(this IAcmeContext context, Func<Directory, Uri> getter, bool optional = false)
        {
            var dir = await context.GetDirectory();
            var uri = getter(dir);
            if (!optional && uri == null)
            {
                throw new NotSupportedException("ACME operation not supported.");
            }

            return uri;
        }
    }

}

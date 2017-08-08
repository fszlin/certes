using Certes.Acme;
using Certes.Pkcs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certes
{
    /// <summary>
    /// Presents the context for ACME operations.
    /// </summary>
    public interface IAcmeContext
    {
        /// <summary>
        /// Fetches an ACME account by key.
        /// </summary>
        /// <param name="key">The account key.</param>
        /// <returns>The account fetched from ACME server.</returns>
        Task<IAccountContext> Account(KeyInfo key);

        /// <summary>
        /// Creates an ACME account.
        /// </summary>
        /// <param name="contact">The contact.</param>
        /// <param name="key">The account key to use with the account, optional.</param>
        /// <returns>
        /// The account created from ACME server.
        /// </returns>
        Task<IAccountContext> Account(IList<string> contact, KeyInfo key = null);
    }
}

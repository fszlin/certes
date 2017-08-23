using Certes.Acme.Resource;
using Certes.Jws;
using Certes.Pkcs;
using System.Threading.Tasks;

namespace Certes.Acme
{
    /// <summary>
    /// Supports ACME account operations.
    /// </summary>
    public interface IAccountContext
    {
        /// <summary>
        /// Gets the account resource.
        /// </summary>
        /// <returns>The account resource.</returns>
        Task<Account> Resource();

        /// <summary>
        /// Gets the orders
        /// </summary>
        /// <returns>The orders.</returns>
        Task<IOrderListContext> Orders();
        
        /// <summary>
        /// Changes the account key.
        /// </summary>
        /// <param name="key">The new key.</param>
        /// <returns>The account context.</returns>
        Task<IAccountContext> ChangeKey(KeyInfo key);

        /// <summary>
        /// Deactivates the current account.
        /// </summary>
        /// <returns>The awaitable.</returns>
        Task<Account> Deactivate();

        /// <summary>
        /// Signs the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        Task<JwsPayload> Sign(object entity);
    }
}

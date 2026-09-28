using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Certes.Acme.Resource;

namespace Certes.Acme
{
    /// <summary>
    /// Supports ACME account operations.
    /// </summary>
    public interface IAccountContext : IResourceContext<Account>
    {
        /// <summary>
        /// Gets the orders
        /// </summary>
        /// <returns>The orders.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<IOrderListContext> Orders(CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the current account.
        /// </summary>
        /// <param name="agreeTermsOfService">Set to <c>true</c> to accept the terms of service.</param>
        /// <param name="contact">The contact infomation.</param>
        /// <returns>The account.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<Account> Update(IList<string> contact = null, bool agreeTermsOfService = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates the current account.
        /// </summary>
        /// <returns>The account deactivated.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<Account> Deactivate(CancellationToken cancellationToken = default);
    }
}

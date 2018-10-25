using System.Collections.Generic;
using System.Threading.Tasks;
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
        Task<IOrderListContext> Orders();

        /// <summary>
        /// Updates the current account.
        /// </summary>
        /// <param name="agreeTermsOfService">Set to <c>true</c> to accept the terms of service.</param>
        /// <param name="contact">The contact infomation.</param>
        /// <returns>The account.</returns>
        Task<Account> Update(IList<string> contact = null, bool agreeTermsOfService = false);

        /// <summary>
        /// Deactivates the current account.
        /// </summary>
        /// <returns>The account deactivated.</returns>
        Task<Account> Deactivate();
    }
}

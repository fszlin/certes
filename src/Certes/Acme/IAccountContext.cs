using Certes.Acme.Resource;
using Certes.Pkcs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certes.Acme
{
    /// <summary>
    /// Presents the context for ACME account operations.
    /// </summary>
    public interface IAccountContext
    {
        /// <summary>
        /// Gets the URI for terms of service.
        /// </summary>
        /// <returns>The terms of service URI.</returns>
        Task<Uri> TermsOfService();

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
        /// Accepts the terms of service.
        /// </summary>
        /// <returns>the context.</returns>
        Task<IAccountContext> AcceptTermsOfService();

        /// <summary>
        /// Updates the account.
        /// </summary>
        /// <param name="contact">The contact.</param>
        /// <returns>The account context.</returns>
        Task<IAccountContext> Update(IList<string> contact);

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
        Task Deactivate();
    }
}

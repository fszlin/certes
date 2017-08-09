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
    /// <seealso cref="Certes.Acme.IAccountContext" />
    public class AccountContext : IAccountContext
    {
        private readonly IAcmeContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountContext"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public AccountContext(IAcmeContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Accepts the terms of service.
        /// </summary>
        /// <returns>
        /// the context.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<IAccountContext> AcceptTermsOfService()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Changes the account key.
        /// </summary>
        /// <param name="key">The new key.</param>
        /// <returns>
        /// The account context.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<IAccountContext> ChangeKey(KeyInfo key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Deactivates the current account.
        /// </summary>
        /// <returns>
        /// The awaitable.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task Deactivate()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the orders
        /// </summary>
        /// <returns>
        /// The orders.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<IOrderListContext> Orders()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the account resource.
        /// </summary>
        /// <returns>
        /// The account resource.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<Account> Resource()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the URI for terms of service.
        /// </summary>
        /// <returns>
        /// The terms of service URI.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<Uri> TermsOfService()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the account.
        /// </summary>
        /// <param name="contact">The contact.</param>
        /// <returns>
        /// The account context.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<IAccountContext> Update(IList<string> contact)
        {
            throw new NotImplementedException();
        }
    }
}

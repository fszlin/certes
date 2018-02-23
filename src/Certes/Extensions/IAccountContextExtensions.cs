using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;

namespace Certes
{
    /// <summary>
    /// Extension methods for <see cref="IAccountContext"/>.
    /// </summary>
    public static class IAccountContextExtensions
    {
        /// <summary>
        /// Deactivates the current account.
        /// </summary>
        /// <returns>The account deactivated.</returns>
        public static Task<Account> Deactivate(
            this Task<IAccountContext> account)
            => account.ContinueWith(a => a.Result.Deactivate()).Unwrap();

        /// <summary>
        /// Gets the location of the account.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <returns>The location URI.</returns>
        public static Task<Uri> Location(this Task<IAccountContext> account)
            => account.ContinueWith(r => r.Result.Location);
    }

}

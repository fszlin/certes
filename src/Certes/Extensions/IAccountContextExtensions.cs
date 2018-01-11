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
        /// Updates the current account.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="contact">The contact infomation.</param>
        /// <param name="agreeTermsOfService">Set to <c>true</c> to accept the terms of service.</param>
        /// <returns>
        /// The account.
        /// </returns>
        public static Task<IAccountContext> Update(
            this Task<IAccountContext> account, IList<string> contact = null, bool agreeTermsOfService = false)
            => account.ContinueWith(a => a.Result.Update(contact, agreeTermsOfService)).Unwrap();

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

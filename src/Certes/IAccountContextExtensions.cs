using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;

namespace Certes
{
    /// <summary>
    /// 
    /// </summary>
    public static class IAccountContextExtensions
    {
        /// <summary>
        /// Updates the specified contact.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="contact">The contact.</param>
        /// <param name="agreeTermsOfService">if set to <c>true</c> [agree terms of service].</param>
        /// <returns></returns>
        public static Task<IAccountContext> Update(
            this Task<IAccountContext> account, IList<string> contact = null, bool agreeTermsOfService = false)
            => account.ContinueWith(a => a.Result.Update(contact, agreeTermsOfService)).Unwrap();

        /// <summary>
        /// Deactivates the specified account.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <returns></returns>
        public static Task<Account> Deactivate(
            this Task<IAccountContext> account)
            => account.ContinueWith(a => a.Result.Deactivate()).Unwrap();

        /// <summary>
        /// Locations the specified account.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <returns></returns>
        public static Task<Uri> Location(this Task<IAccountContext> account)
            => account.ContinueWith(r => r.Result.Location);
    }

}

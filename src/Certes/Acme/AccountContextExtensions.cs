using Certes.Pkcs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certes.Acme
{
    /// <summary>
    /// Helper methods for <see cref="IAccountContext"/>.
    /// </summary>
    public static class AccountContextExtensions
    {
        /// <summary>
        /// Accepts the terms of service.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>The context.</returns>
        public static Task<IAccountContext> AcceptTermsOfService(this Task<IAccountContext> context)
        {
            return context.ContinueWith(ctx => ctx.AcceptTermsOfService()).Unwrap();
        }

        /// <summary>
        /// Updates the account.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="contact">The contact.</param>
        /// <returns>The account context.</returns>
        public static Task<IAccountContext> Update(this Task<IAccountContext> context, IList<string> contact)
        {
            return context.ContinueWith(ctx => ctx.Update(contact)).Unwrap();
        }

        /// <summary>
        /// Changes the account key.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The new key.</param>
        /// <returns>The account context.</returns>
        public static Task<IAccountContext> ChangeKey(this Task<IAccountContext> context, KeyInfo key)
        {
            return context.ContinueWith(ctx => ctx.ChangeKey(key)).Unwrap();
        }
    }
}

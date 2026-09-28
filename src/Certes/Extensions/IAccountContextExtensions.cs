using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
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
        /// <param name="account">The account task.</param>
        /// <param name="cancellationToken">Cancels waiting for the account task and deactivation.</param>
        /// <returns>The account deactivated.</returns>
        public static async Task<Account> Deactivate(
            this Task<IAccountContext> account, CancellationToken cancellationToken = default)
        {
            var context = await WaitForAccount(account, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return await context.Deactivate(cancellationToken);
        }

        /// <summary>
        /// Gets the location of the account.
        /// </summary>
        /// <param name="account">The account.</param>
        /// <param name="cancellationToken">Cancels waiting for the account task, not the task itself.</param>
        /// <returns>The location URI.</returns>
        public static async Task<Uri> Location(this Task<IAccountContext> account, CancellationToken cancellationToken = default)
            => (await WaitForAccount(account, cancellationToken)).Location;

        // Cancels only this wait; the caller must also pass the token when
        // starting the account operation to cancel its underlying HTTP request.
        private static async Task<IAccountContext> WaitForAccount(Task<IAccountContext> account, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!account.IsCompleted && cancellationToken.CanBeCanceled)
            {
                var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
                {
                    await Task.WhenAny(account, cancelled.Task);
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }

            return await account;
        }
    }

}

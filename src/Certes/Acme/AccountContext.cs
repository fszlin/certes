using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the context for ACME account operations.
    /// </summary>
    /// <seealso cref="Certes.Acme.IAccountContext" />
    internal class AccountContext : EntityContext<Account>, IAccountContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountContext" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        public AccountContext(IAcmeContext context, Uri location)
            : base(context, location)
        {
        }

        /// <summary>
        /// Gets the account entity data.
        /// </summary>
        /// <returns>
        /// The account entity data.
        /// </returns>
        public override async Task<Account> Resource()
        {
            var payload = await Context.Sign(new Account(), Location);
            var resp = await Context.HttpClient.Post<Account>(Location, payload, true);
            return resp.Resource;
        }

        /// <summary>
        /// Deactivates the current account.
        /// </summary>
        /// <returns>
        /// The account deactivated.
        /// </returns>
        public async Task<Account> Deactivate()
        {
            var payload = await Context.Sign(new Account { Status = AccountStatus.Deactivated }, Location);
            var resp = await Context.HttpClient.Post<Account>(Location, payload, true);
            return resp.Resource;
        }

        /// <summary>
        /// Gets the order list.
        /// </summary>
        /// <returns>
        /// The orders list.
        /// </returns>
        public async Task<IOrderListContext> Orders()
        {
            var account = await Resource();
            return new OrderListContext(Context, account.Orders);
        }

        /// <summary>
        /// Updates the current account.
        /// </summary>
        /// <param name="contact">The contact infomation.</param>
        /// <param name="agreeTermsOfService">Set to <c>true</c> to accept the terms of service.</param>
        /// <returns>
        /// The account.
        /// </returns>
        public async Task<Account> Update(IList<string> contact = null, bool agreeTermsOfService = false)
        {
            var location = await Context.Account().Location();
            var account = new Account
            {
                Contact = contact
            };

            if (agreeTermsOfService)
            {
                account.TermsOfServiceAgreed = true;
            }

            var payload = await Context.Sign(account, location);
            var response = await Context.HttpClient.Post<Account>(location, payload, true);
            return response.Resource;
        }

        /// <summary>
        /// Post to the new account endpoint.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="body">The payload.</param>
        /// <param name="ensureSuccessStatusCode">if set to <c>true</c>, throw exception if the request failed.</param>
        /// <returns>The ACME response.</returns>
        internal static async Task<AcmeHttpResponse<Account>> NewAccount(
            IAcmeContext context, Account body, bool ensureSuccessStatusCode)
        {
            var endpoint = await context.GetResourceUri(d => d.NewAccount);
            var jws = new JwsSigner(context.AccountKey);
            var payload = jws.Sign(body, url: endpoint, nonce: await context.HttpClient.ConsumeNonce());
            return await context.HttpClient.Post<Account>(endpoint, payload, ensureSuccessStatusCode);
        }
    }
}

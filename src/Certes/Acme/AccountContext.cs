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
        /// Resources this instance.
        /// </summary>
        /// <returns></returns>
        public override async Task<Account> Resource()
        {
            var resp = await NewAccount(Context, new Account.Payload { OnlyReturnExisting = true }, false);
            return resp.Resource;
        }

        /// <summary>
        /// Deactivates the current account.
        /// </summary>
        /// <returns>
        /// The awaitable.
        /// </returns>
        public async Task<Account> Deactivate()
        {
            var payload = await Context.Sign(new { status = AccountStatus.Deactivated }, Location);
            var resp = await Context.HttpClient.Post<Account>(Location, payload, true);
            return resp.Resource;
        }

        /// <summary>
        /// Gets the orders
        /// </summary>
        /// <returns>
        /// The orders.
        /// </returns>
        public async Task<IOrderListContext> Orders()
        {
            var account = await Resource();
            return new OrderListContext(Context, this, account.Orders);
        }

        /// <summary>
        /// Updates the account.
        /// </summary>
        /// <param name="agreeTermsOfService">The agree terms of service.</param>
        /// <param name="contact">The contact.</param>
        /// <returns>
        /// The account context.
        /// </returns>
        public async Task<IAccountContext> Update(IList<string> contact = null, bool agreeTermsOfService = false)
        {
            var location = await Context.Account().Location();
            var account = new Account
            {
                Contact = contact
            };
           
            var payload = await Context.Sign(account, location);
            await Context.HttpClient.Post<Account>(location, payload, true);
            return this;
        }

        /// <summary>
        /// Authorizes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public Task<IAuthorizationContext> Authorize(string value, string type = AuthorizationIdentifierTypes.Dns)
        {
            throw new NotImplementedException();
        }

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

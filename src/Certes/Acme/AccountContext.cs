using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Authz = Certes.Acme.Resource.Authorization;

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
        /// Initializes a new instance of the <see cref="AccountContext" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public AccountContext(IAcmeContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Deactivates the current account.
        /// </summary>
        /// <returns>
        /// The awaitable.
        /// </returns>
        public async Task<Account> Deactivate()
        {
            var location = await context.GetAccountLocation();
            var payload = await context.Sign(new { status = AccountStatus.Deactivated }, location);
            var resp = await context.HttpClient.Post<Account>(location, payload, true);
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
            return new OrderListContext(context, this, account.Orders);
        }

        /// <summary>
        /// Gets the account 
        /// </summary>
        /// <returns>
        /// The account 
        /// </returns>
        public async Task<Account> Resource()
        {
            var location = await context.GetAccountLocation();
            var payload = await context.Sign(new { }, location);
            var resp = await context.HttpClient.Post<Account>(location, payload);
            return resp.Resource;
        }

        /// <summary>
        /// Updates the account.
        /// </summary>
        /// <param name="agreeTermsOfService">The agree terms of service.</param>
        /// <param name="contact">The contact.</param>
        /// <returns>
        /// The account context.
        /// </returns>
        public async Task<IAccountContext> Update(bool agreeTermsOfService = false, IEnumerable<string> contact = null)
        {
            var location = await context.GetAccountLocation();
            var account = new Dictionary<string, object>();
            if (contact != null)
            {
                account["contact"] = contact.ToArray();
            }

            if (agreeTermsOfService)
            {
                account["terms-of-service-agreed"] = true;

                // boulder specific
                var dict = await context.GetDirectory();
                var tosUri = dict.Meta?.TermsOfService;
                if (tosUri != null)
                {
                    account["Agreement"] = tosUri;
                }
            }

            var payload = await context.Sign(account, location);
            await context.HttpClient.Post<Account>(location, payload, true);
            return this;
        }

        /// <summary>
        /// Authorizes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public async Task<IAuthorizationContext> Authorize(string value, string type = AuthorizationIdentifierTypes.Dns)
        {
            var endpoint = await context.GetResourceUri(d => d.NewAuthz);

            var data = new
            {
                identifier = new
                {
                    type,
                    value
                }
            };

            var payload = await context.Sign(data, endpoint);
            var resp = await context.HttpClient.Post<Authz>(endpoint, payload);
            return new AuthorizationContext(context, resp.Location);
        }
    }
}

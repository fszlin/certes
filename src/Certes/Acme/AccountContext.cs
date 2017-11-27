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
        private readonly AcmeContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountContext" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public AccountContext(AcmeContext context)
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
        /// <param name="resource">The account </param>
        /// <returns>
        /// The account context.
        /// </returns>
        public async Task<IAccountContext> Update(Account resource)
        {
            var location = await context.GetAccountLocation();
            var payload = await context.Sign(resource, location);
            await context.HttpClient.Post<Account>(location, payload);
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
                    type = type,
                    value = value
                }
            };

            var payload = await context.Sign(data, endpoint);
            var resp = await context.HttpClient.Post<Authz>(endpoint, payload);
            return new AuthorizationContext(context, resp.Location);
        }
    }
}

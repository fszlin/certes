using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;
using Certes.Pkcs;
using Authz = Certes.Acme.Resource.Authorization;
using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Certes.Acme
{
    /// <summary>
    /// Presents the context for ACME account operations.
    /// </summary>
    /// <seealso cref="Certes.Acme.IAccountContext" />
    public class AccountContext : IAccountContext
    {
        private readonly AcmeContext context;
        private readonly JwsSigner jws;
        private Uri location;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountContext" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public AccountContext(AcmeContext context)
        {
            this.context = context;
            jws = new JwsSigner(context.AccountKey);
        }

        /// <summary>
        /// Deactivates the current account.
        /// </summary>
        /// <returns>
        /// The awaitable.
        /// </returns>
        public async Task<Account> Deactivate()
        {
            var location = await this.DiscoverLocation();
            var payload = this.Sign(new { status = AccountStatus.Deactivated }, location);
            var resp = await this.context.HttpClient.Post<Account>(location, payload);
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
            var location = await this.DiscoverLocation();
            var payload = this.Sign(new { }, location);
            var resp = await this.context.HttpClient.Post<Account>(location, payload);
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
            var payload = Sign(resource, location);
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
            var dir = await context.GetDirectory();
            var endpoint = dir.NewAuthz;
            if (endpoint == null)
            {
                throw new NotSupportedException();
            }

            var data = new
            {
                identifier = new
                {
                    type = type,
                    value = value
                }
            };

            var payload = Sign(data, endpoint);
            var resp = await context.HttpClient.Post<Authz>(endpoint, payload);
            return new AuthorizationContext(context, this, resp.Location);
        }

        /// <summary>
        /// Signs the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public async Task<JwsPayload> Sign(object entity, Uri uri)
        {
            var nonce = await context.HttpClient.ConsumeNonce();
            var location = await this.DiscoverLocation();
            return jws.Sign(entity, location, uri, nonce);
        }

        private async Task<Uri> DiscoverLocation()
        {
            if (location == null)
            {
                var dir = await context.GetDirectory();
                var endpoint = dir.NewAccount;
                if (endpoint == null)
                {
                    throw new NotSupportedException();
                }

                var body = new Dict
                {
                    { "only-return-existing", true },
                };

                var jws = new JwsSigner(context.AccountKey);
                var payload = jws.Sign(body, url: endpoint, nonce: await context.HttpClient.ConsumeNonce());
                var resp = await context.HttpClient.Post<Account>(endpoint, payload);

                location = resp.Location;
            }

            return location;
        }

        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <returns></returns>
        public Task<Uri> GetLocation()
        {
            return this.DiscoverLocation();
        }
    }
}

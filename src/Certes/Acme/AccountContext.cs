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
        private readonly IAccountKey key;
        private readonly JwsSigner jws;
        private Uri location;

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public IAccountKey Key
        {
            get
            {
                return key;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountContext" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        public AccountContext(AcmeContext context, KeyInfo key = null)
        {
            this.context = context;
            this.key = new AccountKey(key);
            jws = new JwsSigner(this.key);
        }

        /// <summary>
        /// Creates the specified contact.
        /// </summary>
        /// <param name="contact">The contact.</param>
        /// <param name="termsOfServiceAgreed">if set to <c>true</c> [terms of service agreed].</param>
        /// <returns></returns>
        public async Task<Account> Create(IList<string> contact, bool termsOfServiceAgreed = false)
        {
            var endpoint = await context.GetResourceEndpoint(ResourceType.NewAccount);
            if (endpoint == null)
            {
                throw new NotSupportedException();
            }

            var body = new Dict
            {
                { "contact", contact },
                { "terms-of-service-agreed", termsOfServiceAgreed },
            };

            var payload = this.Sign(body, endpoint);
            var (location, account, _) = await this.context.Post<Account>(endpoint, payload);

            this.location = location;
            return account;
        }
        
        /// <summary>
        /// Changes the account key.
        /// </summary>
        /// <param name="key">The new key.</param>
        /// <returns>
        /// The account context.
        /// </returns>
        public async Task<IAccountContext> ChangeKey(KeyInfo key = null)
        {
            var endpoint = await this.context.GetResourceEndpoint(ResourceType.KeyChange);
            if (endpoint == null)
            {
                throw new NotSupportedException();
            }

            var location = await this.DiscoverLocation();

            var accountKey = new AccountKey(key);
            var keyChange = new
            {
                account = location,
                newKey = accountKey.JsonWebKey
            };

            var body = this.jws.Sign(keyChange);
            var payload = this.Sign(body, endpoint);
            await this.context.Post<Account>(endpoint, payload);
            return this;
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
            var (_, account, _) = await this.context.Post<Account>(location, payload);
            return account;
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
            var (_, account, _) = await this.context.Post<Account>(location, payload);
            return account;
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
            var (_, account, _) = await context.Post<Account>(location, payload);
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
            var endpoint = await context.GetResourceEndpoint(ResourceType.NewAuthz);
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
            var (url, _, _) = await context.Post<Authz>(endpoint, payload);
            return new AuthorizationContext(context, this, url);
        }

        /// <summary>
        /// Signs the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public async Task<JwsPayload> Sign(object entity, Uri uri)
        {
            var nonce = await context.ConsumeNonce();
            return jws.Sign(entity, location, uri, nonce);
        }

        private async Task<Uri> DiscoverLocation()
        {
            if (location == null)
            {
                var endpoint = await context.GetResourceEndpoint(ResourceType.NewAccount);
                if (endpoint == null)
                {
                    throw new NotSupportedException();
                }

                var body = new Dict
                {
                    { "only-return-existing", true },
                };

                var payload = Sign(body, endpoint);
                var (location, _, _) = await context.Post<Account>(endpoint, payload);

                this.location = location;
            }

            return location;
        }
    }
}

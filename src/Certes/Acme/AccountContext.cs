using Certes.Acme.Resource;
using Certes.Jws;
using Certes.Pkcs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        /// <param name="key">The key.</param>
        public AccountContext(AcmeContext context, KeyInfo key = null)
        {
            this.context = context;
            this.jws = new JwsSigner(new AccountKey(key));
        }

        /// <summary>
        /// Creates the specified contact.
        /// </summary>
        /// <param name="contact">The contact.</param>
        /// <param name="termsOfServiceAgreed">if set to <c>true</c> [terms of service agreed].</param>
        /// <returns></returns>
        public async Task<Account> Create(IList<string> contact, bool termsOfServiceAgreed = false)
        {
            var endpoint = await this.context.GetResourceEndpoint(ResourceType.NewAccount);
            if (endpoint == null)
            {
                throw new NotSupportedException();
            }

            var body = new Dict
            {
                { "contact", contact },
                { "terms-of-service-agreed", termsOfServiceAgreed },
            };

            var payload = this.Sign(body);
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
            var location = await this.DiscoverLocation();

            var accountKey = new AccountKey(key);
            var keyChange = new
            {
                account = location,
                newKey = accountKey.JsonWebKey
            };

            var body = this.jws.Sign(keyChange);
            var payload = this.Sign(body);
            await this.context.Post<Account>(location, payload);
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
            var payload = this.Sign(new { status = AccountStatus.Deactivated });
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
            var account = await this.Resource();
            return new OrderListContext(this.context, this, account.Orders);
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
            var payload = this.Sign(new { });
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
            var payload = this.Sign(resource);
            var (_, account, _) = await this.context.Post<Account>(this.location, payload);
            return this;
        }

        /// <summary>
        /// Signs the specified entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public async Task<JwsPayload> Sign(object entity)
        {
            var nonce = await this.context.ConsumeNonce();
            return jws.Sign(entity, nonce);
        }

        private async Task<Uri> DiscoverLocation()
        {
            if (this.location == null)
            {
                var endpoint = await this.context.GetResourceEndpoint(ResourceType.NewAccount);
                if (endpoint == null)
                {
                    throw new NotSupportedException();
                }

                var body = new Dict
                {
                    { "only-return-existing", true },
                };

                var payload = this.Sign(body);
                var (location, _, _) = await this.context.Post<Account>(endpoint, payload);

                this.location = location;
            }

            return this.location;
        }
    }
}

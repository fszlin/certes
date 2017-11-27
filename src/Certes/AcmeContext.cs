using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Jws;

namespace Certes
{
    /// <summary>
    /// Represents the context for ACME operations.
    /// </summary>
    /// <seealso cref="Certes.IAcmeContext" />
    public class AcmeContext : IAcmeContext
    {
        private Directory directory;
        private IAccountContext accountContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeContext" /> class.
        /// </summary>
        /// <param name="directoryUri">The directory URI.</param>
        /// <param name="accountKey">The account key.</param>
        /// <param name="http">The HTTP client.</param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="directoryUri"/> is <c>null</c>.
        /// </exception>
        public AcmeContext(Uri directoryUri, IAccountKey accountKey = null, IAcmeHttpClient http = null)
        {
            DirectoryUri = directoryUri ?? throw new ArgumentNullException(nameof(directoryUri));
            AccountKey = accountKey ?? new AccountKey();
            HttpClient = http ?? new AcmeHttpClient(directoryUri);
        }

        /// <summary>
        /// Gets the account.
        /// </summary>
        /// <value>
        /// The account.
        /// </value>
        /// <exception cref="NotImplementedException"></exception>
        public IAccountContext Account => accountContext ?? (accountContext = new AccountContext(this));

        /// <summary>
        /// Gets the ACME HTTP client.
        /// </summary>
        /// <value>
        /// The ACME HTTP client.
        /// </value>
        /// <exception cref="NotImplementedException"></exception>
        public IAcmeHttpClient HttpClient { get; }

        /// <summary>
        /// Gets the directory URI.
        /// </summary>
        /// <value>
        /// The directory URI.
        /// </value>
        public Uri DirectoryUri { get; }

        /// <summary>
        /// Gets the account key.
        /// </summary>
        /// <value>
        /// The account key.
        /// </value>
        public IAccountKey AccountKey { get; private set; }

        /// <summary>
        /// Changes the key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public async Task ChangeKey(AccountKey key = null)
        {
            var dir = await GetDirectory();
            var endpoint = dir.KeyChange;
            if (endpoint == null)
            {
                throw new NotSupportedException();
            }

            var location = await this.Account.GetLocation();

            var newKey = key ?? new AccountKey();
            var keyChange = new
            {
                account = location,
                newKey = newKey.JsonWebKey
            };

            var jws = new JwsSigner(AccountKey);
            var body = jws.Sign(keyChange);
            var payload = this.Account.Sign(body, endpoint);
            await this.HttpClient.Post<Account>(endpoint, payload);

            this.AccountKey = newKey;
        }

        /// <summary>
        /// Creates the account.
        /// </summary>
        /// <returns>
        /// The account created.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Account> CreateAccount(IList<string> contact, bool termsOfServiceAgreed = false)
        {
            var dir = await GetDirectory();
            var endpoint = dir.NewAccount;
            if (endpoint == null)
            {
                throw new NotSupportedException();
            }

            var body = new Dictionary<string, object>
            {
                { "contact", contact },
                { "terms-of-service-agreed", termsOfServiceAgreed },
            };

            var jws = new JwsSigner(AccountKey);
            var payload = jws.Sign(body, url: endpoint, nonce: await HttpClient.ConsumeNonce());
            var resp = await this.HttpClient.Post<Account>(endpoint, payload);
            return resp.Resource;
        }

        /// <summary>
        /// Gets the ACME directory.
        /// </summary>
        /// <returns>
        /// The ACME directory.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Directory> GetDirectory()
        {
            if (directory == null)
            {
                var resp = await this.HttpClient.Get<Directory>(DirectoryUri);
                directory = resp.Resource;
            }

            return directory;
        }

        /// <summary>
        /// Revokes the certificate.
        /// </summary>
        /// <returns>
        /// The awaitable.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task RevokeCertificate()
        {
            throw new NotImplementedException();
        }
    }
}

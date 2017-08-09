using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Json;
using Certes.Jws;
using Certes.Pkcs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Linq;

using Dict = System.Collections.Generic.Dictionary<string, object>;
using System.Threading;

namespace Certes
{
    /// <summary>
    /// Presents the context for ACME operations.
    /// </summary>
    /// <seealso cref="Certes.IAcmeContext" />
    public class AcmeContext : IAcmeContext
    {
        private readonly static Lazy<HttpClient> SharedHttp = new Lazy<HttpClient>(() => new HttpClient());

        /// <summary>
        /// The directory URI.
        /// </summary>
        private readonly Uri directoryUri;
        private readonly IContextFactory contextFactory;
        private readonly JsonSerializerSettings jsonSettings = JsonUtil.CreateSettings();

        private Directory directory;
        private string nonce = null;
        private JwsSigner jws;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeContext"/> class.
        /// </summary>
        public AcmeContext() : this(WellKnownServers.LetsEncrypt)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeContext"/> class.
        /// </summary>
        /// <param name="directoryUri">The directory URI.</param>
        public AcmeContext(Uri directoryUri)
             : this(WellKnownServers.LetsEncrypt, new ContextFactory())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeContext" /> class.
        /// </summary>
        /// <param name="directoryUri">The directory URI.</param>
        /// <param name="contextFactory">The context factory.</param>
        public AcmeContext(Uri directoryUri, IContextFactory contextFactory)
        {
            this.directoryUri = directoryUri;
            this.contextFactory = contextFactory;
        }

        /// <summary>
        /// Fetches an ACME account by key.
        /// </summary>
        /// <param name="key">The account key.</param>
        /// <returns>
        /// The account fetched from ACME server.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<IAccountContext> Account(KeyInfo key)
        {
            this.jws = new JwsSigner(new AccountKey(key));
            var payload = this.Sign(
                new Dict
                {
                    { "only-return-existing", true }
                });
            
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates an ACME account.
        /// </summary>
        /// <param name="contact">The contact.</param>
        /// <param name="key">The account key to use with the account, optional.</param>
        /// <returns>
        /// The account created from ACME server.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<IAccountContext> Account(IList<string> contact, KeyInfo key = null)
        {
            this.jws = new JwsSigner(new AccountKey(key));
            throw new NotImplementedException();
        }

        private async ValueTask<Directory> GetDirectory()
        {
            if (this.directory == null)
            {
                await this.FetchDirectory();
            }

            return directory;
        }

        private async Task FetchDirectory()
        {
            var response = await SharedHttp.Value.GetAsync(this.directoryUri);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception();
            }

            var dirJson = await response.Content.ReadAsStringAsync();
            this.directory = JsonConvert.DeserializeObject<Directory>(dirJson);
            this.nonce = response.Headers.GetValues("Replay-Nonce").FirstOrDefault();
        }

        private async Task FetchNonce()
        {
            var dir = await GetDirectory();
            if (dir.NewAccount == null)
            {
                throw new InvalidOperationException();
            }

            var response = await SharedHttp.Value.SendAsync(new HttpRequestMessage
            {
                RequestUri = dir.NewAccount,
                Method = HttpMethod.Head,
            });

            this.nonce = response.Headers.GetValues("Replay-Nonce").FirstOrDefault();
            if (this.nonce == null)
            {
                throw new Exception();
            }
        }

        private async Task<string> ConsumeNonce()
        {
            var nonce = Interlocked.Exchange(ref this.nonce, null);
            while (nonce == null)
            {
                await this.FetchNonce();
                nonce = Interlocked.Exchange(ref this.nonce, null);
            }

            return nonce;
        }

        private object Sign(object entity)
        {
            if (jws == null)
            {
                throw new InvalidOperationException();
            }

            return jws.Sign(entity, this.nonce);
        }
    }
}

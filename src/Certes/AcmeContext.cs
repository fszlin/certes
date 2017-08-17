using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Json;
using Certes.Jws;
using Certes.Pkcs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace Certes
{
    /// <summary>
    /// Presents the context for ACME operations.
    /// </summary>
    /// <seealso cref="Certes.IAcmeContext" />
    public class AcmeContext : IAcmeContext
    {
        private const string MimeJson = "application/json";
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
        public async Task<IAccountContext> Account(KeyInfo key)
        {
            var dir = await this.GetDirectory();
            if (dir.NewAccount == null)
            {
                throw new InvalidOperationException();
            }

            this.jws = new JwsSigner(new AccountKey(key));
            var payload = this.Sign(
                new Dict
                {
                    { "only-return-existing", true }
                });
            
            var bodyJson = JsonConvert.SerializeObject(payload, Formatting.None, jsonSettings);
            var content = new StringContent(bodyJson, Encoding.UTF8, MimeJson);

            var response = await SharedHttp.Value.PostAsync(dir.NewAccount, content);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception();
            }

            var accountJson = await response.Content.ReadAsStringAsync();
            var account = JsonConvert.DeserializeObject<Account>(accountJson);

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

        private async Task<Directory> GetDirectory()
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
            if (dir.NewNonce == null)
            {
                throw new InvalidOperationException();
            }

            var response = await SharedHttp.Value.SendAsync(new HttpRequestMessage
            {
                RequestUri = dir.NewNonce,
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

        private async Task<JwsPayload> Sign(object entity)
        {
            if (jws == null)
            {
                throw new InvalidOperationException();
            }

            var nonce = await this.ConsumeNonce();
            return jws.Sign(entity, nonce);
        }
    }
}

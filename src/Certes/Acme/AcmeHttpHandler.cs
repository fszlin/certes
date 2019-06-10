using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Certes.Json;
using Certes.Jws;
using Certes.Properties;
using Newtonsoft.Json;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the HTTP handler for communicating with ACME server.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class AcmeHttpHandler : IAcmeHttpHandler, IDisposable
    {
        private const string MimeJson = "application/json";

        private readonly static Lazy<HttpClient> SharedHttp = new Lazy<HttpClient>(()=>HttpClientFactory.CreateInternalHttpClient());
        private readonly HttpClient http;
        private readonly Uri serverUri;
        private readonly bool shouldDisposeHttp;

        private string nonce;
        private AcmeDirectory directory;

        private readonly JsonSerializerSettings jsonSettings = JsonUtil.CreateSettings();

        /// <summary>
        /// Gets the ACME server URI.
        /// </summary>
        /// <value>
        /// The ACME server URI.
        /// </value>
        public Uri ServerUri
        {
            get
            {
                return serverUri;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeHttpHandler"/> class.
        /// </summary>
        /// <param name="serverUri">The server URI.</param>
        public AcmeHttpHandler(Uri serverUri)
            : this(serverUri, SharedHttp.Value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeHttpHandler"/> class.
        /// </summary>
        /// <param name="serverUri">The server URI.</param>
        /// <param name="httpMessageHandler">The HTTP message handler.</param>
        [Obsolete("Use AcmeHttpHandler(Uri, HttpClient) instead.")]
        public AcmeHttpHandler(Uri serverUri, HttpMessageHandler httpMessageHandler = null)
            : this(serverUri, httpMessageHandler == null ? SharedHttp.Value : new HttpClient(httpMessageHandler))
        {
            this.shouldDisposeHttp = httpMessageHandler != null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeHttpHandler"/> class.
        /// </summary>
        /// <param name="serverUri">The server URI.</param>
        /// <param name="httpClient">The HTTP client.</param>
        public AcmeHttpHandler(Uri serverUri, HttpClient httpClient)
        {
            this.http = httpClient ?? SharedHttp.Value;
            this.serverUri = serverUri;
            this.shouldDisposeHttp = false;
        }

        /// <summary>
        /// Gets the ACME resource URI.
        /// </summary>
        /// <param name="resourceType">Type of the ACME resource.</param>
        /// <returns>The ACME resource URI</returns>
        public async Task<Uri> GetResourceUri(string resourceType)
        {
            await FetchDirectory(false);
            var resourceUri =
                ResourceTypes.NewRegistration == resourceType ? this.directory.NewReg :
                ResourceTypes.NewAuthorization == resourceType ? this.directory.NewAuthz :
                ResourceTypes.NewCertificate == resourceType ? this.directory.NewCert :
                ResourceTypes.RevokeCertificate == resourceType ? this.directory.RevokeCert :
                ResourceTypes.KeyChange == resourceType ? this.directory.KeyChange :
                null;

            if (resourceUri == null)
            {
                throw new AcmeException(string.Format(Strings.ErrorUnsupportedResourceType, resourceType));
            }

            return resourceUri;
        }

        /// <summary>
        /// Performs HTTP GET to <paramref name="uri"/>.
        /// </summary>
        /// <typeparam name="T">The resource entity type.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <returns>The ACME response.</returns>
        public async Task<AcmeResponse<T>> Get<T>(Uri uri)
        {
            var resp = await http.GetAsync(uri);
            var result = await ReadResponse<T>(resp);
            return result;
        }

        /// <summary>
        /// Performs HTTP POST to <paramref name="uri"/>.
        /// </summary>
        /// <typeparam name="T">The resource entity type.</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="keyPair">The signing key pair.</param>
        /// <returns>The ACME response.</returns>
        public async Task<AcmeResponse<T>> Post<T>(Uri uri, T entity, IAccountKey keyPair)
        {
            await FetchDirectory(false);

            var content = await GenerateRequestContent(entity, keyPair);
            var resp = await http.PostAsync(uri, content);
            var result = await ReadResponse<T>(resp, (entity as EntityBase)?.Resource);
            return result;
        }

        /// <summary>
        /// Encodes the specified entity for ACME requests.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="keyPair">The key pair.</param>
        /// <param name="nonce">The nonce.</param>
        /// <returns>The encoded JSON.</returns>
        private static object Encode(object entity, IAccountKey keyPair, string nonce)
        {
            var encoder = new JwsSigner(keyPair.SignatureKey);
            return encoder.Sign(entity, nonce);
        }

        private async Task FetchDirectory(bool force)
        {
            if (this.directory == null || force)
            {
                var uri = serverUri;
                var resp = await this.Get<AcmeDirectory>(uri);
                this.directory = resp.Data;
            }
        }

        private async Task<StringContent> GenerateRequestContent(object entity, IAccountKey keyPair)
        {
            var nonce = await this.GetNonce();
            var body = Encode(entity, keyPair, nonce);
            var bodyJson = JsonConvert.SerializeObject(body, Formatting.None, jsonSettings);

            return new StringContent(bodyJson, Encoding.UTF8, MimeJson);
        }

        private async Task<AcmeResponse<T>> ReadResponse<T>(HttpResponseMessage response, string resourceType = null)
        {
            var data = new AcmeResponse<T>();

            ParseHeaders(data, response);
            if (IsJsonMedia(response.Content?.Headers.ContentType?.MediaType))
            {
                var json = await response.Content.ReadAsStringAsync();
                data.Json = json;
                if (response.IsSuccessStatusCode)
                {
                    data.Data = JsonConvert.DeserializeObject<T>(json, jsonSettings);
                    var entity = data.Data as EntityBase;
                    if (resourceType != null && entity != null && string.IsNullOrWhiteSpace(entity.Resource))
                    {
                        entity.Resource = resourceType;
                    }
                }
                else
                {
                    data.Error = JsonConvert.DeserializeObject<AcmeError>(json, jsonSettings);
                }

                // take the replay-nonce from JOSN response
                // it appears the nonces returned with certificate are invalid
                nonce = data.ReplayNonce;
            }
            else if (response.Content?.Headers.ContentLength > 0)
            {
                data.Raw = await response.Content.ReadAsByteArrayAsync();
            }

            data.HttpStatus = response.StatusCode;
            return data;
        }

        private static void ParseHeaders<T>(AcmeResponse<T> data, HttpResponseMessage response)
        {
            data.Location = response.Headers.Location;

            if (response.Headers.Contains("Replay-Nonce"))
            {
                data.ReplayNonce = response.Headers.GetValues("Replay-Nonce").Single();
            }

            // TODO: Support Retry-After header

            if (response.Headers.Contains("Link"))
            {

                data.Links = response.Headers.GetValues("Link")?
                    .Select(h =>
                    {
                        var segments = h.Split(';');
                        var url = segments[0].Substring(1, segments[0].Length - 2);
                        var rel = segments.Skip(1)
                            .Select(s => s.Trim())
                            .Where(s => s.StartsWith("rel=", StringComparison.OrdinalIgnoreCase))
                            .Select(r =>
                            {
                                var relType = r.Split('=')[1];
                                return relType.Substring(1, relType.Length - 2);
                            })
                            .First();

                        return new RelLink
                        {
                            Rel = rel,
                            Uri = new Uri(url)
                        };
                    })
                    .ToArray();
            }

            data.ContentType = response.Content?.Headers.ContentType?.MediaType;
        }

        private static bool IsJsonMedia(string mediaType)
        {
            if (mediaType != null && mediaType.StartsWith("application/"))
            {
                return mediaType
                    .Substring("application/".Length)
                    .Split('+')
                    .Any(t => t == "json");
            }

            return false;
        }

        private async Task<string> GetNonce()
        {
            var nonce = Interlocked.Exchange(ref this.nonce, null);
            while (nonce == null)
            {
                await this.FetchDirectory(true);
                nonce = Interlocked.Exchange(ref this.nonce, null);
            }

            return nonce;
        }

        #region IDisposable Support
        private bool disposedValue = false;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && shouldDisposeHttp)
                {
                    http?.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}

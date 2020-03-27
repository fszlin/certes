using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Json;
using Certes.Properties;
using Newtonsoft.Json;

namespace Certes.Acme
{
    /// <summary>
    /// HTTP client handling ACME operations.
    /// </summary>
    /// <seealso cref="Certes.Acme.IAcmeHttpClient" />
    public class AcmeHttpClient : IAcmeHttpClient
    {
        private const string MimeJoseJson = "application/jose+json";

        private readonly static JsonSerializerSettings jsonSettings = JsonUtil.CreateSettings();
        private readonly static Lazy<HttpClient> SharedHttp = new Lazy<HttpClient>(CreateHttpClient);
        private readonly Lazy<HttpClient> http;

        private Uri newNonceUri;
        private readonly Uri directoryUri;
        private string nonce;

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>
        /// The HTTP client.
        /// </value>
        private HttpClient Http { get => http.Value; }

        /// <summary>
        /// Creates an instance of HttpClient configured with default settings.
        /// </summary>
        internal static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            ConfigureDefaultUserAgentHeader(client);
            return client;
        }

        /// <remarks>
        /// ACME clients MUST send a User-Agent header field, in accordance with
        /// [RFC7231]. This header field SHOULD include the name and version of
        /// the ACME software in addition to the name and version of the
        /// underlying HTTP client software.
        /// </remarks>
        private static void ConfigureDefaultUserAgentHeader(HttpClient client)
        {
            var certesVersion = typeof(AcmeHttpClient).GetTypeInfo().Assembly.GetName().Version;
            var netVersion
#if NETSTANDARD1_3
                = "1.3";
#else
                = Environment.Version;
#endif
            lock (client)
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"Certes/{certesVersion} .NET/{netVersion}");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeHttpClient" /> class.
        /// </summary>
        /// <param name="directoryUri">The ACME directory URI.</param>
        /// <param name="http">The HTTP.</param>
        /// <exception cref="ArgumentNullException">directoryUri</exception>
        public AcmeHttpClient(Uri directoryUri, HttpClient http = null)
        {
            this.directoryUri = directoryUri;

            if (http != null)
            {
                ConfigureDefaultUserAgentHeader(http);
            }

            this.http = http == null ? SharedHttp : new Lazy<HttpClient>(() => http);
        }

        /// <summary>
        /// Gets the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public async Task<AcmeHttpResponse<T>> Get<T>(Uri uri)
        {
            using (var response = await Http.GetAsync(uri))
            {
                return await ProcessResponse<T>(response);
            }
        }

        /// <summary>
        /// Posts the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        public async Task<AcmeHttpResponse<T>> Post<T>(Uri uri, object payload)
        {
            var payloadJson = JsonConvert.SerializeObject(payload, Formatting.None, jsonSettings);
            var content = new StringContent(payloadJson, Encoding.UTF8, MimeJoseJson);
            // boulder will reject the request if sending charset=utf-8
            content.Headers.ContentType.CharSet = null;
            using (var response = await Http.PostAsync(uri, content))
            {
                return await ProcessResponse<T>(response);
            }
        }

        /// <summary>
        /// Gets the nonce for next request.
        /// </summary>
        /// <returns>
        /// The nonce.
        /// </returns>
        public async Task<string> ConsumeNonce()
        {
            var nonce = Interlocked.Exchange(ref this.nonce, null);
            while (nonce == null)
            {
                await FetchNonce();
                nonce = Interlocked.Exchange(ref this.nonce, null);
            }

            return nonce;
        }

        private async Task<AcmeHttpResponse<T>> ProcessResponse<T>(HttpResponseMessage response)
        {
            var location = response.Headers.Location;
            var resource = default(T);
            var links = default(ILookup<string, Uri>);
            var error = default(AcmeError);

            if (response.Headers.Contains("Replay-Nonce"))
            {
                nonce = response.Headers.GetValues("Replay-Nonce").Single();
            }

            if (response.Headers.Contains("Link"))
            {
                links = response.Headers.GetValues("Link")?
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

                        return (
                            Rel: rel,
                            Uri: new Uri(url)
                        );
                    })
                    .ToLookup(l => l.Rel, l => l.Uri);
            }

            if (response.IsSuccessStatusCode)
            {
                if (IsJsonMedia(response.Content?.Headers.ContentType?.MediaType))
                {
                    var json = await response.Content.ReadAsStringAsync();
                    resource = JsonConvert.DeserializeObject<T>(json);
                }
                else if (typeof(T) == typeof(string))
                {
                    object content = await response.Content.ReadAsStringAsync();
                    resource = (T)content;
                }
            }
            else
            {
                if (IsJsonMedia(response.Content?.Headers.ContentType.MediaType))
                {
                    var json = await response.Content.ReadAsStringAsync();
                    error = JsonConvert.DeserializeObject<AcmeError>(json);
                }
            }

            return new AcmeHttpResponse<T>(location, resource, links, error);
        }

        private async Task FetchNonce()
        {
            newNonceUri = newNonceUri ?? (await Get<Directory>(directoryUri)).Resource.NewNonce;
            var response = await Http.SendAsync(new HttpRequestMessage
            {
                RequestUri = newNonceUri,
                Method = HttpMethod.Head,
            });

            if (!response.Headers.TryGetValues("Replay-Nonce", out var values))
            {
                throw new AcmeException(Strings.ErrorFetchNonce);
            }

            nonce = values.FirstOrDefault();
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
    }
}

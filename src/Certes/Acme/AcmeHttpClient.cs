using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Certes.Json;
using Newtonsoft.Json;

namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Certes.Acme.IAcmeHttpClient" />
    internal class AcmeHttpClient : IAcmeHttpClient
    {
        private const string MimeJoseJson = "application/jose+json";
        private const string MimeJson = "application/json";

        private readonly static JsonSerializerSettings jsonSettings = JsonUtil.CreateSettings();
        private readonly static Lazy<HttpClient> SharedHttp = new Lazy<HttpClient>(() => new HttpClient());
        private readonly Uri directoryUri;
        private readonly Lazy<HttpClient> http;

        private string nonce;

        /// <summary>
        /// Initializes a new instance of the <see cref="AcmeHttpClient"/> class.
        /// </summary>
        /// <param name="directoryUri">The directory URI.</param>
        /// <param name="http">The HTTP.</param>
        /// <exception cref="ArgumentNullException">directoryUri</exception>
        public AcmeHttpClient(Uri directoryUri, HttpClient http = null)
        {
            this.directoryUri = directoryUri ?? throw new ArgumentNullException(nameof(directoryUri));
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
            using (var response = await http.Value.GetAsync(uri))
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
            using (var response = await http.Value.PostAsync(uri, content))
            {
                return await ProcessResponse<T>(response);
            }
        }

        /// <summary>
        /// Consumes the nonce.
        /// </summary>
        /// <returns></returns>
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
                if (IsJsonMedia(response.Content?.Headers.ContentType.MediaType))
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
            var response = await http.Value.SendAsync(new HttpRequestMessage
            {
                RequestUri = directoryUri,
                Method = HttpMethod.Head,
            });

            nonce = response.Headers.GetValues("Replay-Nonce").FirstOrDefault();
            if (nonce == null)
            {
                throw new Exception();
            }
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

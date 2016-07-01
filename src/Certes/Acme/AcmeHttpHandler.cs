using Certes.Json;
using Certes.Jws;
using Certes.Pkcs;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using AcmeDirectory = System.Collections.Generic.Dictionary<string, System.Uri>;

namespace Certes.Acme
{
    public class AcmeHttpHandler : IDisposable
    {
        private const string MimeJson = "application/json";

        private readonly HttpClient http;
        private readonly Uri serverUri;

        private string nonce;
        private AcmeDirectory directory;

        private readonly JsonSerializerSettings jsonSettings = JsonUtil.CreateSettings();
        
        public AcmeHttpHandler(Uri serverUri, HttpMessageHandler httpMessageHandler = null)
        {
            this.http = httpMessageHandler == null ? new HttpClient() : new HttpClient(httpMessageHandler);
            this.serverUri = serverUri;
        }

        public async Task<Uri> GetResourceUri(string resourceType)
        {
            await FetchDirectory(false);
            return this.directory[resourceType];
        }

        public async Task<AcmeRespone<T>> Get<T>(Uri uri)
        {
            var resp = await http.GetAsync(uri);
            var result = await ReadResponse<T>(resp);
            return result;
        }

        public async Task<AcmeRespone<T>> Post<T>(Uri uri, T entity, IAccountKey keyPair)
            where T : EntityBase
        {
            await FetchDirectory(false);

            var content = GenerateRequestContent(entity, keyPair);
            var resp = await http.PostAsync(uri, content);
            var result = await ReadResponse<T>(resp, entity.Resource);
            return result;
        }

        private async Task FetchDirectory(bool force)
        {
            if (this.directory == null || this.nonce == null || force)
            {
                var uri = serverUri;
                var resp = await this.Get<AcmeDirectory>(uri);
                this.directory = resp.Data;
            }
        }

        private StringContent GenerateRequestContent(EntityBase entity, IAccountKey keyPair)
        {
            var unprotectedHeader = new
            {
                alg = keyPair.Algorithm.ToJwsAlgorithm(),
                jwk = keyPair.Jwk
            };

            var protectedHeader = new
            {
                nonce = this.nonce
            };

            var entityJson = JsonConvert.SerializeObject(entity, Formatting.None, jsonSettings);
            var protectedHeaderJson = JsonConvert.SerializeObject(protectedHeader, Formatting.None, jsonSettings);

            var payloadEncoded = JwsConvert.ToBase64String(Encoding.UTF8.GetBytes(entityJson));
            var protectedHeaderEncoded = JwsConvert.ToBase64String(Encoding.UTF8.GetBytes(protectedHeaderJson));

            var signature = $"{protectedHeaderEncoded}.{payloadEncoded}";
            var signatureBytes = Encoding.ASCII.GetBytes(signature);
            var signedSignatureBytes = keyPair.SignData(signatureBytes);
            var signedSignatureEncoded = JwsConvert.ToBase64String(signedSignatureBytes);

            var body = new
            {
                header = unprotectedHeader,
                @protected = protectedHeaderEncoded,
                payload = payloadEncoded,
                signature = signedSignatureEncoded
            };
            
            var bodyJson = JsonConvert.SerializeObject(body, Formatting.None, jsonSettings);
            return new StringContent(bodyJson, Encoding.ASCII, MimeJson);
        }

        private async Task<AcmeRespone<T>> ReadResponse<T>(HttpResponseMessage response, string resourceType = null)
        {
            var data = new AcmeRespone<T>();
            if (IsJsonMedia(response.Content.Headers.ContentType.MediaType))
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
            }
            else if (response.Content.Headers.ContentLength > 0)
            {
                data.Raw = await response.Content.ReadAsByteArrayAsync();
            }

            ParseHeaders(data, response);
            this.nonce = data.ReplayNonce;
            return data;
        }

        private static void ParseHeaders<T>(AcmeRespone<T> data, HttpResponseMessage response)
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

            data.ContentType = response.Content.Headers.ContentType.MediaType;
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

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    http?.Dispose();
                }
                
                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}

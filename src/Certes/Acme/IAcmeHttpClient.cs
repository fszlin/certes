using System;
using System.Threading.Tasks;

namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAcmeHttpClient
    {
        /// <summary>
        /// Consumes the nonce.
        /// </summary>
        /// <returns></returns>
        Task<string> ConsumeNonce();

        /// <summary>
        /// Posts the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="payload">The payload.</param>
        /// <returns></returns>
        Task<AcmeHttpResponse<T>> Post<T>(Uri uri, object payload);

        /// <summary>
        /// Gets the specified URI.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        Task<AcmeHttpResponse<T>> Get<T>(Uri uri);
    }

    internal static class IAcmeHttpClientExtensions
    {
        internal static async Task<AcmeHttpResponse<T>> Post<T>(this IAcmeHttpClient client, Uri uri, object payload, bool ensureSuccessStatusCode)
        {
            var resp = await client.Post<T>(uri, payload);
            if (ensureSuccessStatusCode && resp.Error != null)
            {
                throw new Exception(resp.Error.Detail);
            }

            return resp;
        }
    }
}

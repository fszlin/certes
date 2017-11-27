using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Certes.Jws;
using Newtonsoft.Json;

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
}

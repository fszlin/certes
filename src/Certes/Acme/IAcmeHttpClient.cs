using System;
using System.Threading.Tasks;
using Certes.Properties;

namespace Certes.Acme
{
    /// <summary>
    /// Supports HTTP operations for ACME servers.
    /// </summary>
    public interface IAcmeHttpClient
    {
        /// <summary>
        /// Gets the nonce for next request.
        /// </summary>
        /// <returns>
        /// The nonce.
        /// </returns>
        Task<string> ConsumeNonce();

        /// <summary>
        /// Posts the data to the specified URI.
        /// </summary>
        /// <typeparam name="T">The type of expected result</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="payload">The payload.</param>
        /// <returns>The response from ACME server.</returns>
        Task<AcmeHttpResponse<T>> Post<T>(Uri uri, object payload);

        /// <summary>
        /// Gets the data from specified URI.
        /// </summary>
        /// <typeparam name="T">The type of expected result</typeparam>
        /// <param name="uri">The URI.</param>
        /// <returns>The response from ACME server.</returns>
        Task<AcmeHttpResponse<T>> Get<T>(Uri uri);
    }

    /// <summary>
    /// Extension methods for <see cref="IAcmeHttpClient"/>.
    /// </summary>
    internal static class IAcmeHttpClientExtensions
    {
        /// <summary>
        /// Posts the data to the specified URI.
        /// </summary>
        /// <typeparam name="T">The type of expected result</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="uri">The URI.</param>
        /// <param name="payload">The payload.</param>
        /// <param name="ensureSuccessStatusCode">if set to <c>true</c>, throw exception if the request failed.</param>
        /// <returns>
        /// The response from ACME server.
        /// </returns>
        /// <exception cref="Exception">
        /// If the HTTP request failed and <paramref name="ensureSuccessStatusCode"/> is <c>true</c>.
        /// </exception>
        internal static async Task<AcmeHttpResponse<T>> Post<T>(this IAcmeHttpClient client, Uri uri, object payload, bool ensureSuccessStatusCode)
        {
            var resp = await client.Post<T>(uri, payload);
            if (ensureSuccessStatusCode && resp.Error != null)
            {
                throw new AcmeRequestException(
                    string.Format(Strings.ErrorFetchResource, uri),
                    resp.Error);
            }

            return resp;
        }
    }
}

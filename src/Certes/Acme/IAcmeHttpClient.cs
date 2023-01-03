using System;
using System.Threading.Tasks;
using Certes.Jws;
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
        /// <param name="context">The context.</param>
        /// <param name="location">The URI.</param>
        /// <param name="entity">The payload.</param>
        /// <param name="ensureSuccessStatusCode">if set to <c>true</c>, throw exception if the request failed.</param>
        /// <returns>
        /// The response from ACME server.
        /// </returns>
        /// <exception cref="Exception">
        /// If the HTTP request failed and <paramref name="ensureSuccessStatusCode"/> is <c>true</c>.
        /// </exception>
        internal static async Task<AcmeHttpResponse<T>> Post<T>(this IAcmeHttpClient client,
            IAcmeContext context,
            Uri location,
            object entity,
            bool ensureSuccessStatusCode)
        {

            var payload = await context.Sign(entity, location);
            var response = await client.Post<T>(location, payload);
            var retryCount = context.BadNonceRetryCount;
            while (response.Error?.Status == System.Net.HttpStatusCode.BadRequest &&
                response.Error.Type?.CompareTo("urn:ietf:params:acme:error:badNonce") == 0 &&
                retryCount-- > 0)
            {
                payload = await context.Sign(entity, location);
                response = await client.Post<T>(location, payload);
            }

            if (ensureSuccessStatusCode && response.Error != null)
            {
                throw new AcmeRequestException(
                    string.Format(Strings.ErrorFetchResource, location),
                    response.Error);
            }

            return response;
        }

        /// <summary>
        /// Posts the data to the specified URI.
        /// </summary>
        /// <typeparam name="T">The type of expected result</typeparam>
        /// <param name="client">The client.</param>
        /// <param name="jwsSigner">The jwsSigner used to sign the payload.</param>
        /// <param name="location">The URI.</param>
        /// <param name="entity">The payload.</param>
        /// <param name="ensureSuccessStatusCode">if set to <c>true</c>, throw exception if the request failed.</param>
        /// <param name="retryCount">Number of retries on badNonce errors (default = 1)</param>
        /// <returns>
        /// The response from ACME server.
        /// </returns>
        /// <exception cref="Exception">
        /// If the HTTP request failed and <paramref name="ensureSuccessStatusCode"/> is <c>true</c>.
        /// </exception>
        internal static async Task<AcmeHttpResponse<T>> Post<T>(this IAcmeHttpClient client,
            JwsSigner jwsSigner,
            Uri location,
            object entity,
            bool ensureSuccessStatusCode,
            int retryCount = 1)
        {
            var payload = jwsSigner.Sign(entity, url: location, nonce: await client.ConsumeNonce());
            var response = await client.Post<T>(location, payload);

            while (response.Error?.Status == System.Net.HttpStatusCode.BadRequest &&
                response.Error.Type?.CompareTo("urn:ietf:params:acme:error:badNonce") == 0 &&
                retryCount-- > 0)
            {
                payload = jwsSigner.Sign(entity, url: location, nonce: await client.ConsumeNonce());
                response = await client.Post<T>(location, payload);
            }

            if (ensureSuccessStatusCode && response.Error != null)
            {
                throw new AcmeRequestException(
                    string.Format(Strings.ErrorFetchResource, location),
                    response.Error);
            }

            return response;
        }
    }
}

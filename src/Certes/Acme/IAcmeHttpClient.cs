using System;
using System.Threading.Tasks;
using System.Threading;
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
        /// <param name="cancellationToken">Cancels nonce acquisition.</param>
        Task<string> ConsumeNonce(CancellationToken cancellationToken = default);

        /// <summary>
        /// Posts the data to the specified URI.
        /// </summary>
        /// <typeparam name="T">The type of expected result</typeparam>
        /// <param name="uri">The URI.</param>
        /// <param name="payload">The payload.</param>
        /// <returns>The response from ACME server.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<AcmeHttpResponse<T>> Post<T>(Uri uri, object payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the data from specified URI.
        /// </summary>
        /// <typeparam name="T">The type of expected result</typeparam>
        /// <param name="uri">The URI.</param>
        /// <returns>The response from ACME server.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<AcmeHttpResponse<T>> Get<T>(Uri uri, CancellationToken cancellationToken = default);
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
        /// <param name="cancellationToken">Cancels this operation.</param>
        internal static async Task<AcmeHttpResponse<T>> Post<T>(this IAcmeHttpClient client,
            IAcmeContext context,
            Uri location,
            object entity,
            bool ensureSuccessStatusCode,
            CancellationToken cancellationToken)
        {

            cancellationToken.ThrowIfCancellationRequested();
            var payload = await context.Sign(entity, location, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var response = await client.Post<T>(location, payload, cancellationToken);
            var retryCount = context.BadNonceRetryCount;
            while (response.Error?.Status == System.Net.HttpStatusCode.BadRequest &&
                response.Error.Type?.CompareTo("urn:ietf:params:acme:error:badNonce") == 0 &&
                retryCount-- > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                payload = await context.Sign(entity, location, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                response = await client.Post<T>(location, payload, cancellationToken);
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
        /// <param name="cancellationToken">Cancels this operation.</param>
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
            int retryCount,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var payload = jwsSigner.Sign(entity, url: location, nonce: await client.ConsumeNonce(cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            var response = await client.Post<T>(location, payload, cancellationToken);

            while (response.Error?.Status == System.Net.HttpStatusCode.BadRequest &&
                response.Error.Type?.CompareTo("urn:ietf:params:acme:error:badNonce") == 0 &&
                retryCount-- > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                payload = jwsSigner.Sign(entity, url: location, nonce: await client.ConsumeNonce(cancellationToken));
                cancellationToken.ThrowIfCancellationRequested();
                response = await client.Post<T>(location, payload, cancellationToken);
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

using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Certes.Acme;
using Certes.Acme.Resource;

namespace Certes
{
    /// <summary>
    /// Extension methods for <see cref="IAuthorizationContext"/>.
    /// </summary>
    public static class IAuthorizationContextExtensions
    {
        /// <summary>
        /// Gets the HTTP challenge.
        /// </summary>
        /// <param name="authorizationContext">The authorization context.</param>
        /// <returns>The HTTP challenge, <c>null</c> if no HTTP challenge available.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        public static Task<IChallengeContext> Http(this IAuthorizationContext authorizationContext, CancellationToken cancellationToken = default) =>
            authorizationContext.Challenge(ChallengeTypes.Http01, cancellationToken);

        /// <summary>
        /// Gets the DNS challenge.
        /// </summary>
        /// <param name="authorizationContext">The authorization context.</param>
        /// <returns>The DNS challenge, <c>null</c> if no DNS challenge available.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        public static Task<IChallengeContext> Dns(this IAuthorizationContext authorizationContext, CancellationToken cancellationToken = default) =>
            authorizationContext.Challenge(ChallengeTypes.Dns01, cancellationToken);

        /// <summary>
        /// Gets the draft DNS persistent authorization challenge.
        /// </summary>
        /// <param name="authorizationContext">The authorization context.</param>
        /// <param name="cancellationToken">Cancels this operation.</param>
        /// <returns>The challenge, or <c>null</c> if it is not offered.</returns>
        public static Task<IChallengeContext> DnsPersist(this IAuthorizationContext authorizationContext, CancellationToken cancellationToken = default) =>
            authorizationContext.Challenge(ChallengeTypes.DnsPersist01, cancellationToken);

        /// <summary>
        /// Gets the TLS ALPN challenge.
        /// </summary>
        /// <param name="authorizationContext">The authorization context.</param>
        /// <returns>The TLS ALPN challenge, <c>null</c> if no TLS ALPN challenge available.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        public static Task<IChallengeContext> TlsAlpn(this IAuthorizationContext authorizationContext, CancellationToken cancellationToken = default) =>
            authorizationContext.Challenge(ChallengeTypes.TlsAlpn01, cancellationToken);

        /// <summary>
        /// Gets a challenge by type.
        /// </summary>
        /// <param name="authorizationContext">The authorization context.</param>
        /// <param name="type">The challenge type.</param>
        /// <returns>The challenge, <c>null</c> if no challenge found.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        public static async Task<IChallengeContext> Challenge(this IAuthorizationContext authorizationContext, string type, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var challenges = await authorizationContext.Challenges(cancellationToken);
            return challenges.FirstOrDefault(c => c.Type == type);
        }
    }
}

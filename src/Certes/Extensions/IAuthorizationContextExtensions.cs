using System.Linq;
using System.Threading.Tasks;
using Certes.Acme;

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
        public static Task<IChallengeContext> Http(this IAuthorizationContext authorizationContext)
            => authorizationContext.ChallengeByType(ChallengeTypes.Http01);

        /// <summary>
        /// Gets the DNS challenge.
        /// </summary>
        /// <param name="authorizationContext">The authorization context.</param>
        /// <returns>The DNS challenge, <c>null</c> if no DNS challenge available.</returns>
        public static Task<IChallengeContext> Dns(this IAuthorizationContext authorizationContext)
            => authorizationContext.ChallengeByType(ChallengeTypes.Dns01);

        /// <summary>
        /// Gets a challenge by type.
        /// </summary>
        /// <param name="authorizationContext">The authorization context.</param>
        /// <param name="type">The challenge type.</param>
        /// <returns>The challenge, <c>null</c> if no challenge found.</returns>
        private static async Task<IChallengeContext> ChallengeByType(this IAuthorizationContext authorizationContext, string type)
        {
            var challenges = await authorizationContext.Challenges();
            return challenges.FirstOrDefault(c => c.Type == type);
        }
    }
}

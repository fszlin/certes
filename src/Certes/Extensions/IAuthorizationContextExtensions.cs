using System.Linq;
using System.Threading.Tasks;

namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    public static class IAuthorizationContextExtensions
    {
        /// <summary>
        /// HTTPs the specified authorization context.
        /// </summary>
        /// <param name="authorizationContext">The authorization context.</param>
        /// <returns></returns>
        public static Task<IChallengeContext> Http(this IAuthorizationContext authorizationContext)
            => authorizationContext.ChallengeByType(ChallengeTypes.Http01);

        /// <summary>
        /// DNSs the specified authorization context.
        /// </summary>
        /// <param name="authorizationContext">The authorization context.</param>
        /// <returns></returns>
        public static Task<IChallengeContext> Dns(this IAuthorizationContext authorizationContext)
            => authorizationContext.ChallengeByType(ChallengeTypes.Dns01);

        private static async Task<IChallengeContext> ChallengeByType(this IAuthorizationContext authorizationContext, string type)
        {
            var challenges = await authorizationContext.Challenges();
            return challenges.FirstOrDefault(c => c.Type == type);
        }
    }
}

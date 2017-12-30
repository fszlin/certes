using System.Linq;
using System.Threading.Tasks;
using Certes.Acme;

namespace Certes
{
    /// <summary>
    /// 
    /// </summary>
    public static class AuthorizationContextExtensions
    {
        /// <summary>
        /// HTTPs the specified authorization context.
        /// </summary>
        /// <param name="authorizationContext">The authorization context.</param>
        /// <returns></returns>
        public static async Task<IChallengeContext> Http(this IAuthorizationContext authorizationContext)
        {
            var challenges = await authorizationContext.Challenges();
            return challenges.FirstOrDefault(c => c.Type == ChallengeTypes.Http01);
        }
    }
}

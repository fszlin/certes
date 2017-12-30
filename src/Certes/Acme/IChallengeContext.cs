using System.Threading.Tasks;
using Certes.Acme.Resource;

namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    public interface IChallengeContext : IResourceContext<AuthorizationIdentifierChallenge>
    {
        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        string Type { get; }

        /// <summary>
        /// Gets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        string Token {get;}

        /// <summary>
        /// Gets the key authorization.
        /// </summary>
        /// <value>
        /// The key authorization.
        /// </value>
        string KeyAuthorization { get; }

        /// <summary>
        /// Acknowledges the ACME server the challenge is ready for validation
        /// </summary>
        /// <returns>The new status.</returns>
        Task<AuthorizationIdentifierChallengeStatus> Validate();
    }
}

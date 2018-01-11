using System.Collections.Generic;
using System.Threading.Tasks;
using Authz = Certes.Acme.Resource.Authorization;

namespace Certes.Acme
{
    /// <summary>
    /// Supports ACME authorization operations.
    /// </summary>
    public interface IAuthorizationContext : IResourceContext<Authz>
    {
        /// <summary>
        /// Gets the challenges for this authorization.
        /// </summary>
        /// <returns>The list fo challenges.</returns>
        Task<IEnumerable<IChallengeContext>> Challenges();

        /// <summary>
        /// Deactivates this authzorization.
        /// </summary>
        /// <returns>
        /// The authorization deactivated.
        /// </returns>
        Task<Authz> Deactivate();
    }
}

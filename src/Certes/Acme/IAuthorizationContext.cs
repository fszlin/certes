using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Certes.Acme.Resource;

namespace Certes.Acme
{
    /// <summary>
    /// Supports ACME authorization operations.
    /// </summary>
    public interface IAuthorizationContext : IResourceContext<Authorization>
    {
        /// <summary>
        /// Gets the challenges for this authorization.
        /// </summary>
        /// <returns>The list fo challenges.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<IEnumerable<IChallengeContext>> Challenges(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deactivates this authzorization.
        /// </summary>
        /// <returns>
        /// The authorization deactivated.
        /// </returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<Authorization> Deactivate(CancellationToken cancellationToken = default);
    }
}

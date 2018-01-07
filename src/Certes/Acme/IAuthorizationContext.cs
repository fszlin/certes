using System.Collections.Generic;
using System.Threading.Tasks;
using Authz = Certes.Acme.Resource.Authorization;

namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAuthorizationContext : IResourceContext<Authz>
    {
        /// <summary>
        /// Gets the challenges for this authorization.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<IChallengeContext>> Challenges();

        /// <summary>
        /// Deactivates this instance.
        /// </summary>
        /// <returns></returns>
        Task<Authz> Deactivate();
    }
}

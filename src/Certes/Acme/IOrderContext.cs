using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme.Resource;

namespace Certes.Acme
{
    /// <summary>
    /// Presents the context for ACME order operations.
    /// </summary>
    public interface IOrderContext : IResourceContext<Order>
    {
        /// <summary>
        /// Authorizationses this instance.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<IAuthorizationContext>> Authorizations();
    }
}

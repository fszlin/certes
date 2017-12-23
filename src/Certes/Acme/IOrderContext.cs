using System.Collections.Generic;
using System.Threading.Tasks;
using Certes.Acme.Resource;

namespace Certes.Acme
{
    /// <summary>
    /// Presents the context for ACME order operations.
    /// </summary>
    public interface IOrderContext
    {
        /// <summary>
        /// Gets the account resource.
        /// </summary>
        /// <returns>The account resource.</returns>
        Task<Order> Resource();

        /// <summary>
        /// Authorizationses this instance.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<IAuthorizationContext>> Authorizations();
    }
}

using System.Threading.Tasks;

namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    public interface IAuthorizationContext
    {
        /// <summary>
        /// Resources this instance.
        /// </summary>
        /// <returns></returns>
        Task<Resource.Authorization> Resource();
    }
}

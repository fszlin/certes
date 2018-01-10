using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certes.Acme
{
    /// <summary>
    /// Presents the context for ACME order list operations.
    /// </summary>
    public interface IOrderListContext
    {
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        Task<IEnumerable<IOrderContext>> Orders();
    }
}

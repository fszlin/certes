using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certes.Acme
{
    /// <summary>
    /// Presents the context for ACME order list operations.
    /// </summary>
    public interface IOrderListContext : IEnumerable<IOrderContext>
    {
        /// <summary>
        /// Creates a new order the specified CSR.
        /// </summary>
        /// <param name="csr">The CSR.</param>
        /// <param name="notBefore">The not before timestamp.</param>
        /// <param name="notAfter">The not after timestamp.</param>
        /// <returns>The created order.</returns>
        Task<IOrderContext> New(string csr, DateTimeOffset notBefore, DateTimeOffset notAfter);
    }
}

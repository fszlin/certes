using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="Certes.Acme.IOrderListContext" />
    public class OrderListContext : IOrderListContext
    {
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public IEnumerator<IOrderContext> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new order the specified CSR.
        /// </summary>
        /// <param name="csr">The CSR.</param>
        /// <param name="notBefore">The not before timestamp.</param>
        /// <param name="notAfter">The not after timestamp.</param>
        /// <returns>
        /// The created order.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<IOrderContext> New(string csr, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
        /// </returns>
        /// <exception cref="NotImplementedException"></exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}

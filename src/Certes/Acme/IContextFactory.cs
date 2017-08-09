using System;
using System.Collections.Generic;
using System.Text;

namespace Certes.Acme
{
    /// <summary>
    /// 
    /// </summary>
    public interface IContextFactory
    {
        /// <summary>
        /// Creates the account context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        IAccountContext CreateAccountContext(IAcmeContext context);
    }
}

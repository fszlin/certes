using System;
using System.Threading.Tasks;

namespace Certes
{
    /// <summary>
    /// Presents the context for ACME operations.
    /// </summary>
    public interface IAcmeContext
    {
        /// <summary>
        /// Gets the URI for terms of service.
        /// </summary>
        /// <returns>
        /// The terms of service URI.
        /// </returns>
        Task<Uri> TermsOfService();
    }
}

using System.Threading.Tasks;
using System.Threading;

namespace Certes.Acme
{
    /// <summary>
    /// Supports ACME challenge operations.
    /// </summary>
    public interface IChallengeContext : IResourceContext<Resource.Challenge>
    {
        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        string Type { get; }

        /// <summary>
        /// Gets the token.
        /// </summary>
        /// <value>
        /// The token, or <c>null</c> for tokenless challenges such as dns-persist-01.
        /// </value>
        string Token { get; }

        /// <summary>
        /// Gets the key authorization string.
        /// </summary>
        /// <value>
        /// The key authorization string.
        /// </value>
        /// <exception cref="System.InvalidOperationException">dns-persist-01 has no key authorization.</exception>
        string KeyAuthz { get; }

        /// <summary>
        /// Acknowledges the ACME server the challenge is ready for validation.
        /// </summary>
        /// <returns>The challenge.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        Task<Resource.Challenge> Validate(CancellationToken cancellationToken = default);
    }
}

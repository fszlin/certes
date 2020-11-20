using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the context for ACME authorization operations.
    /// </summary>
    /// <seealso cref="Certes.Acme.IAuthorizationContext" />
    internal class AuthorizationContext : EntityContext<Authorization>, IAuthorizationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationContext"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        public AuthorizationContext(
            IAcmeContext context,
            Uri location)
            : base(context, location)
        {
        }

        /// <summary>
        /// Gets the challenges for this authorization.
        /// </summary>
        /// <returns>
        /// The list fo challenges.
        /// </returns>
        public async Task<IEnumerable<IChallengeContext>> Challenges()
        {
            var authz = await Resource();
            return authz
                .Challenges?
                .Select(c => new ChallengeContext(Context, c.Url, c.Type, c.Token)) ??
                Enumerable.Empty<IChallengeContext>();
        }

        /// <summary>
        /// Deactivates this authzorization.
        /// </summary>
        /// <returns>
        /// The authorization deactivated.
        /// </returns>
        public async Task<Authorization> Deactivate()
        {
            var payload = new Authorization { Status = AuthorizationStatus.Deactivated };
            var resp = await Context.HttpClient.Post<Authorization>(Context, Location, payload, true);
            return resp.Resource;
        }
    }
}

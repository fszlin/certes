using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Authz = Certes.Acme.Resource.Authorization;

namespace Certes.Acme
{
    internal class AuthorizationContext : EntityContext<Authz>, IAuthorizationContext
    {
        public AuthorizationContext(
            IAcmeContext context,
            Uri location)
            : base(context, location)
        {
        }

        public async Task<IEnumerable<IChallengeContext>> Challenges()
        {
            var authz = await Resource();
            return authz
                .Challenges?
                .Select(c => new ChallengeContext(Context, c.Url, c.Type, c.Token)) ??
                Enumerable.Empty<IChallengeContext>();
        }

        /// <summary>
        /// Deactivates the current account.
        /// </summary>
        /// <returns>
        /// The awaitable.
        /// </returns>
        public async Task<Authz> Deactivate()
        {
            var payload = await Context.Sign(new Authz { Status = AuthorizationStatus.Deactivated }, Location);
            var resp = await Context.HttpClient.Post<Authz>(Location, payload, true);
            return resp.Resource;
        }
    }
}

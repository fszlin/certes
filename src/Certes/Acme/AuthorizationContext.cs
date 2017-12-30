using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    }
}

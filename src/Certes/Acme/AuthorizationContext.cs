using System;
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
    }
}

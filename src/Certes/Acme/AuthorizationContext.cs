using System;
using System.Threading.Tasks;
using Authz = Certes.Acme.Resource.Authorization;

namespace Certes.Acme
{
    internal class AuthorizationContext : IAuthorizationContext
    {
        private readonly IAcmeContext context;
        private readonly Uri location;

        public AuthorizationContext(
            IAcmeContext context,
            Uri location)
        {
            this.context = context;
            this.location = location;
        }

        public async Task<Authz> Resource()
        {
            var resp = await context.HttpClient.Get<Authz>(location);
            return resp.Resource;
        }
    }
}

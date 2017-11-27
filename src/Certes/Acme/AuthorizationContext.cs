using System;
using System.Threading.Tasks;
using Authz = Certes.Acme.Resource.Authorization;

namespace Certes.Acme
{
    internal class AuthorizationContext : IAuthorizationContext
    {
        private readonly AcmeContext context;
        private readonly IAccountContext account;
        private readonly Uri location;

        public AuthorizationContext(
            AcmeContext context,
            IAccountContext account,
            Uri location)
        {
            this.context = context;
            this.account = account;
            this.location = location;
        }

        public async Task<Authz> Resource()
        {
            var resp = await context.HttpClient.Get<Authz>(location);
            return resp.Resource;
        }
    }
}

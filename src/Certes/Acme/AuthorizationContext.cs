using System;

namespace Certes.Acme
{
    internal class AuthorizationContext
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
    }
}
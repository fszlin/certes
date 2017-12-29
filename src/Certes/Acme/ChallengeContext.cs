using System;
using Certes.Acme.Resource;
using Certes.Jws;

namespace Certes.Acme
{
    internal class ChallengeContext : EntityContext<AuthorizationIdentifierChallenge>, IChallengeContext
    {
        public ChallengeContext(
            AcmeContext context,
            Uri location,
            string type,
            string token)
            : base(context, location)
        {
            Type = type;
            Token = token;
        }

        public string Type { get; }

        public string Token { get; }

        public string KeyAuthorization
        {
            get
            {
                var jwkThumbprintEncoded = Context.AccountKey.Thumbprint();
                return $"{Token}.{jwkThumbprintEncoded}";
            }
        }
    }
}

using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;

namespace Certes.Acme
{
    internal class ChallengeContext : EntityContext<AuthorizationIdentifierChallenge>, IChallengeContext
    {
        public ChallengeContext(
            IAcmeContext context,
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

        public async Task<AuthorizationIdentifierChallenge> Validate()
        {
            var payload = await Context.Sign(
                new AuthorizationIdentifierChallenge {
                    KeyAuthorization = KeyAuthorization
                }, Location);
            var resp = await Context.HttpClient.Post<AuthorizationIdentifierChallenge>(Location, payload, true);
            return resp.Resource;
        }
    }
}

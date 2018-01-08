using System;
using System.Threading.Tasks;
using Certes.Crypto;
using Certes.Jws;

namespace Certes.Acme
{
    internal class ChallengeContext : EntityContext<Resource.Challenge>, IChallengeContext
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

        public string KeyAuthz => Context.AccountKey.KeyAuthorization(Token);

        public async Task<Resource.Challenge> Validate()
        {
            var payload = await Context.Sign(
                new Resource.Challenge {
                    KeyAuthorization = Context.AccountKey.KeyAuthorization(Token)
                }, Location);
            var resp = await Context.HttpClient.Post<Resource.Challenge>(Location, payload, true);
            return resp.Resource;
        }
    }
}

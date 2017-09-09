using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;

namespace Certes.Acme
{
    internal class ChallengeContext
    {
        private readonly AcmeContext context;
        private readonly IAccountContext account;
        private readonly Uri location;

        public ChallengeContext(
            AcmeContext context,
            IAccountContext account,
            Uri location)
        {
            this.context = context;
            this.account = account;
            this.location = location;
        }

        public async Task<AuthorizationIdentifierChallenge> Resource()
        {
            var (_, challenge, _) = await this.context.Get<AuthorizationIdentifierChallenge>(location);
            return challenge;
        }

        /// <summary>
        /// Computes the key authorization.
        /// </summary>
        /// <returns></returns>
        public async Task<string> ComputeKeyAuthorization()
        {
            // TODO: cache token
            var challenge = await Resource();
            var jwkThumbprintEncoded = account.Key.Thumbprint();
            var token = challenge.Token;
            return $"{token}.{jwkThumbprintEncoded}";
        }
    }
}

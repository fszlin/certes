using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Jws;

namespace Certes.Acme
{
    internal class ChallengeContext
    {
        private readonly AcmeContext context;
        private readonly Uri location;

        public ChallengeContext(
            AcmeContext context,
            Uri location)
        {
            this.context = context;
            this.location = location;
        }

        public async Task<AuthorizationIdentifierChallenge> Resource()
        {
            var resp = await this.context.HttpClient.Get<AuthorizationIdentifierChallenge>(location);
            return resp.Resource;
        }

        /// <summary>
        /// Computes the key authorization.
        /// </summary>
        /// <returns></returns>
        public async Task<string> ComputeKeyAuthorization()
        {
            // TODO: cache token
            var challenge = await Resource();
            var jwkThumbprintEncoded = context.AccountKey.Thumbprint();
            var token = challenge.Token;
            return $"{token}.{jwkThumbprintEncoded}";
        }
    }
}

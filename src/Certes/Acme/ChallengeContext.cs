﻿using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the context for ACME challenge operations.
    /// </summary>
    /// <seealso cref="Certes.Acme.IChallengeContext" />
    internal class ChallengeContext : EntityContext<Challenge>, IChallengeContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChallengeContext"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        /// <param name="type">The type.</param>
        /// <param name="token">The token.</param>
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

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public string Type { get; }

        /// <summary>
        /// Gets the token.
        /// </summary>
        /// <value>
        /// The token.
        /// </value>
        public string Token { get; }

        /// <summary>
        /// Gets the key authorization string.
        /// </summary>
        /// <value>
        /// The key authorization string.
        /// </value>
        public string KeyAuthz => Context.AccountKey.KeyAuthorization(Token);

        /// <summary>
        /// Acknowledges the ACME server the challenge is ready for validation.
        /// </summary>
        /// <returns>
        /// The challenge.
        /// </returns>
        public async Task<Challenge> Validate()
        {
            var resp = await Context.HttpClient.Post<Challenge>(Context, Location, new {}, true);
            return resp.Resource;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Json;
using Certes.Jws;

namespace Certes.Acme
{
    /// <summary>
    /// Represents the context for ACME account operations.
    /// </summary>
    /// <seealso cref="Certes.Acme.IAccountContext" />
    internal class AccountContext : EntityContext<Account>, IAccountContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountContext" /> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        public AccountContext(IAcmeContext context, Uri location)
            : base(context, location)
        {
        }

        /// <summary>
        /// Deactivates the current account.
        /// </summary>
        /// <returns>
        /// The account deactivated.
        /// </returns>
        public async Task<Account> Deactivate()
        {
            var payload = await Context.Sign(new Account { Status = AccountStatus.Deactivated }, Location);
            var resp = await Context.HttpClient.Post<Account>(Location, payload, true);
            return resp.Resource;
        }

        /// <summary>
        /// Gets the order list.
        /// </summary>
        /// <returns>
        /// The orders list.
        /// </returns>
        public async Task<IOrderListContext> Orders()
        {
            var account = await Resource();
            return new OrderListContext(Context, account.Orders);
        }

        /// <summary>
        /// Updates the current account.
        /// </summary>
        /// <param name="contact">The contact infomation.</param>
        /// <param name="agreeTermsOfService">Set to <c>true</c> to accept the terms of service.</param>
        /// <returns>
        /// The account.
        /// </returns>
        public async Task<Account> Update(IList<string> contact = null, bool agreeTermsOfService = false)
        {
            var location = await Context.Account().Location();
            var account = new Account
            {
                Contact = contact
            };

            if (agreeTermsOfService)
            {
                account.TermsOfServiceAgreed = true;
            }

            var payload = await Context.Sign(account, location);
            var response = await Context.HttpClient.Post<Account>(location, payload, true);
            return response.Resource;
        }

        /// <summary>
        /// Post to the new account endpoint.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="body">The payload.</param>
        /// <param name="ensureSuccessStatusCode">if set to <c>true</c>, throw exception if the request failed.</param>
        /// <param name="eabKeyId">Optional key identifier, if using external account binding.</param>
        /// <param name="eabKey">Optional EAB key, if using external account binding.</param>
        /// <param name="eabKeyAlg">Optional EAB key algorithm, if using external account binding, defaults to HS256 if not specified</param>
        /// <returns>The ACME response.</returns>
        internal static async Task<AcmeHttpResponse<Account>> NewAccount(
            IAcmeContext context, Account body, bool ensureSuccessStatusCode,
            string eabKeyId = null, string eabKey = null, string eabKeyAlg = null)
        {
            var endpoint = await context.GetResourceUri(d => d.NewAccount);
            var jws = new JwsSigner(context.AccountKey);

            if (eabKeyId != null && eabKey != null)
            {
                var header = new
                {
                    alg = eabKeyAlg?.ToUpper() ?? "HS256",
                    kid = eabKeyId,
                    url = endpoint
                };

                var headerJson = Newtonsoft.Json.JsonConvert.SerializeObject(header, Newtonsoft.Json.Formatting.None, JsonUtil.CreateSettings());
                var protectedHeaderBase64 = JwsConvert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(headerJson));

                var accountKeyBase64 = JwsConvert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(
                        Newtonsoft.Json.JsonConvert.SerializeObject(context.AccountKey.JsonWebKey, Newtonsoft.Json.Formatting.None)
                        )
                    );

                var signingBytes = System.Text.Encoding.ASCII.GetBytes($"{protectedHeaderBase64}.{accountKeyBase64}");

                // eab signature is the hash of the header and account key, using the eab key
                byte[] signatureHash;

                switch (header.alg)
                {
                    case "HS512":
                        signatureHash = new HMACSHA512(JwsConvert.FromBase64String(eabKey)).ComputeHash(signingBytes);
                        break;
                    case "HS384":
                        signatureHash = new HMACSHA384(JwsConvert.FromBase64String(eabKey)).ComputeHash(signingBytes);
                        break;
                    default:
                        signatureHash = new HMACSHA256(JwsConvert.FromBase64String(eabKey)).ComputeHash(signingBytes);
                        break;   
                }
                    
                var signatureBase64 = JwsConvert.ToBase64String(signatureHash);

                body.ExternalAccountBinding = new
                {
                    Protected = protectedHeaderBase64,
                    Payload = accountKeyBase64,
                    Signature = signatureBase64
                };
            }

            var payload = jws.Sign(body, url: endpoint, nonce: await context.HttpClient.ConsumeNonce());
            return await context.HttpClient.Post<Account>(endpoint, payload, ensureSuccessStatusCode);
        }
    }
}

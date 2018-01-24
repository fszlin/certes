using System;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Pkcs;

namespace Certes
{
    /// <summary>
    /// Extension methods for <see cref="IOrderContext"/>.
    /// </summary>
    public static class IOrderContextExtensions
    {
        /// <summary>
        /// Finalizes the certificate order.
        /// </summary>
        /// <param name="context">The order context.</param>
        /// <param name="csr">The CSR in DER.</param>
        /// <param name="key">The private key for the certificate.</param>
        /// <returns>
        /// The order finalized.
        /// </returns>
        public static async Task<Order> Finalize(this IOrderContext context, CsrInfo csr, IKey key)
        {
            var builder = new CertificationRequestBuilder(key);
            foreach (var authzCtx in await context.Authorizations())
            {
                var authz = await authzCtx.Resource();
                builder.SubjectAlternativeNames.Add(authz.Identifier.Value);
            }

            foreach (var (name, value) in csr.Fields)
            {
                builder.AddName(name, value);
            }

            if (string.IsNullOrWhiteSpace(csr.CommonName))
            {
                builder.AddName("CN", builder.SubjectAlternativeNames[0]);
            }

            return await context.Finalize(builder.Generate());
        }

        /// <summary>
        /// Generates the certifcate for the order.
        /// </summary>
        /// <param name="context">The order context.</param>
        /// <param name="csr">The CSR in DER.</param>
        /// <param name="key">The private key for the certificate.</param>
        /// <returns>
        /// The certificate generated.
        /// </returns>
        public static async Task<CertificateInfo> Generate(this IOrderContext context, CsrInfo csr, IKey key = null)
        {
            var order = await context.Resource();
            if (order.Status == OrderStatus.Pending)
            {
                if (key == null)
                {
                    key = KeyFactory.NewKey(KeyAlgorithm.RS256);
                }

                order = await context.Finalize(csr, key);
            }

            if (order.Status != OrderStatus.Valid)
            {
                throw new Exception("Invalid order status.");
            }

            if (key == null)
            {
                throw new Exception("Certificate key must be provied for finalized order.");
            }

            var pem = await context.Download();

            return new CertificateInfo(pem, key);
        }

        /// <summary>
        /// Gets the authorization by identifier.
        /// </summary>
        /// <param name="context">The order context.</param>
        /// <param name="value">The identifier value.</param>
        /// <param name="type">The identifier type.</param>
        /// <returns>The authorization found.</returns>
        public static async Task<IAuthorizationContext> Authorization(this IOrderContext context, string value, IdentifierType type = IdentifierType.Dns)
        {
            var wildcard = value.StartsWith("*.");
            if (wildcard)
            {
                value = value.Substring(2);
            }

            foreach (var authzCtx in await context.Authorizations())
            {
                var authz = await authzCtx.Resource();
                if (string.Equals(authz.Identifier.Value, value, StringComparison.OrdinalIgnoreCase) &&
                    wildcard == authz.Wildcard.GetValueOrDefault() &&
                    authz.Identifier.Type == type)
                {
                    return authzCtx;
                }
            }

            return null;
        }
    }
}

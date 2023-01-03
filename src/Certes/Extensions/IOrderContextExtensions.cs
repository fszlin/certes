using System;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Pkcs;
using Certes.Properties;

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
        /// <param name="csr">The CSR.</param>
        /// <param name="key">The private key for the certificate.</param>
        /// <returns>
        /// The order finalized.
        /// </returns>
        public static async Task<Order> Finalize(this IOrderContext context, CsrInfo csr, IKey key)
        {
            var builder = await context.CreateCsr(key);

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
        /// Creates CSR from the order.
        /// </summary>
        /// <param name="context">The order context.</param>
        /// <param name="key">The private key.</param>
        /// <returns>The CSR.</returns>
        public static async Task<CertificationRequestBuilder> CreateCsr(this IOrderContext context, IKey key)
        {
            var builder = new CertificationRequestBuilder(key);
            var order = await context.Resource();
            foreach (var identifier in order.Identifiers)
            {
                builder.SubjectAlternativeNames.Add(identifier.Value);
            }

            return builder;
        }

        /// <summary>
        /// Finalizes and download the certifcate for the order.
        /// </summary>
        /// <param name="context">The order context.</param>
        /// <param name="csr">The CSR.</param>
        /// <param name="key">The private key for the certificate.</param>
        /// <param name="retryCount">Number of retries when the Order is in 'processing' state. (default = 1)</param>
        /// <param name="preferredChain">The preferred Root Certificate.</param>
        /// <returns>
        /// The certificate generated.
        /// </returns>
        public static async Task<CertificateChain> Generate(this IOrderContext context, CsrInfo csr, IKey key, string preferredChain = null, int retryCount = 1)
        {
            var order = await context.Resource();
            if (order.Status != OrderStatus.Ready && // draft-11
                order.Status != OrderStatus.Pending) // pre draft-11
            {
                throw new AcmeException(string.Format(Strings.ErrorInvalidOrderStatusForFinalize, order.Status));
            }

            order = await context.Finalize(csr, key);

            while ((order == null || order.Status == OrderStatus.Processing) && retryCount-- > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(context.RetryAfter, 1)));
                order = await context.Resource();
            }

            if (order?.Status != OrderStatus.Valid)
            {
                throw new AcmeException(Strings.ErrorFinalizeFailed);
            }

            return await context.Download(preferredChain);
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

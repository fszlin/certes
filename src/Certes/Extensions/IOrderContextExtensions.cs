using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
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
        private const int DefaultRetryCount = 60;
        private const int MaxCommonNameLength = 64;
        private const int MaxRetryAfterSeconds = 15 * 60;

        /// <summary>
        /// Finalizes the certificate order.
        /// </summary>
        /// <param name="context">The order context.</param>
        /// <param name="csr">The CSR.</param>
        /// <param name="key">The private key for the certificate.</param>
        /// <returns>
        /// The order finalized.
        /// </returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        public static async Task<Order> Finalize(this IOrderContext context, CsrInfo csr, IKey key, CancellationToken cancellationToken = default)
        {
            var builder = await context.CreateCsr(key, cancellationToken);

            foreach (var (name, value) in csr.Fields)
            {
                builder.AddName(name, value);
            }

            if (string.IsNullOrWhiteSpace(csr.CommonName))
            {
                // Use the first DNS name that fits the 64-character CN limit (RFC 5280).
                // IP addresses are not placed in the CN; the CSR then relies on SANs only.
                var commonName = builder.SubjectAlternativeNames.FirstOrDefault(
                    n => n != null && n.Length <= MaxCommonNameLength && !IpAddressUtil.TryParse(n, out _));
                if (commonName != null)
                {
                    builder.AddName("CN", commonName);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            return await context.Finalize(builder.Generate(), cancellationToken);
        }

        /// <summary>
        /// Creates CSR from the order.
        /// </summary>
        /// <param name="context">The order context.</param>
        /// <param name="key">The private key.</param>
        /// <returns>The CSR.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        public static async Task<CertificationRequestBuilder> CreateCsr(this IOrderContext context, IKey key, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var builder = new CertificationRequestBuilder(key);
            var order = await context.Resource(cancellationToken);
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
        /// <param name="retryCount">
        /// Maximum number of polling retries shared by both phases: waiting for the order
        /// to become ready before finalize, and waiting for pending/processing states after
        /// finalize. (default = 60; negative values are treated as zero)
        /// </param>
        /// <param name="preferredChain">The preferred Root Certificate.</param>
        /// <returns>
        /// The certificate generated.
        /// </returns>
        /// <param name="cancellationToken">Cancels requests and polling delays.</param>
        public static async Task<CertificateChain> Generate(this IOrderContext context, CsrInfo csr, IKey key, string preferredChain = null, int retryCount = DefaultRetryCount, CancellationToken cancellationToken = default)
            => await Generate(context, csr, key, preferredChain, retryCount, Task.Delay, cancellationToken);

        internal static async Task<CertificateChain> Generate(
            IOrderContext context,
            CsrInfo csr,
            IKey key,
            string preferredChain,
            int retryCount,
            Func<TimeSpan, CancellationToken, Task> delay,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var order = await context.Resource(cancellationToken);
            if (order.Status != OrderStatus.Ready &&
                order.Status != OrderStatus.Pending)
            {
                throw new AcmeException(string.Format(Strings.ErrorInvalidOrderStatusForFinalize, order.Status));
            }

            retryCount = Math.Max(retryCount, 0);
            while (order?.Status == OrderStatus.Pending && retryCount-- > 0)
            {
                await delay(TimeSpan.FromSeconds(Math.Min(Math.Max(context.RetryAfter, 1), MaxRetryAfterSeconds)), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                order = await context.Resource(cancellationToken);
            }

            if (order?.Status != OrderStatus.Ready)
            {
                throw new AcmeException(string.Format(
                    Strings.ErrorInvalidOrderStatusForFinalize,
                    order?.Status?.ToString() ?? "Unknown"));
            }

            cancellationToken.ThrowIfCancellationRequested();
            order = await context.Finalize(csr, key, cancellationToken);

            while ((order == null || order.Status == OrderStatus.Pending || order.Status == OrderStatus.Processing) && retryCount-- > 0)
            {
                await delay(TimeSpan.FromSeconds(Math.Min(Math.Max(context.RetryAfter, 1), MaxRetryAfterSeconds)), cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                order = await context.Resource(cancellationToken);
            }

            if (order?.Status != OrderStatus.Valid)
            {
                throw new AcmeException(Strings.ErrorFinalizeFailed);
            }

            cancellationToken.ThrowIfCancellationRequested();
            return await context.Download(preferredChain, cancellationToken);
        }

        /// <summary>
        /// Gets the authorization by identifier.
        /// </summary>
        /// <param name="context">The order context.</param>
        /// <param name="value">The identifier value.</param>
        /// <param name="type">The identifier type.</param>
        /// <returns>The authorization found.</returns>
        /// <param name="cancellationToken">Cancels this operation.</param>
        public static async Task<IAuthorizationContext> Authorization(this IOrderContext context, string value, IdentifierType type = IdentifierType.Dns, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var wildcard = value.StartsWith("*.");
            if (wildcard)
            {
                value = value.Substring(2);
            }

            foreach (var authzCtx in await context.Authorizations(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var authz = await authzCtx.Resource(cancellationToken);
                if (authz.Identifier.Type == type &&
                    wildcard == authz.Wildcard.GetValueOrDefault() &&
                    IdentifierValueEquals(type, authz.Identifier.Value, value))
                {
                    return authzCtx;
                }
            }

            return null;
        }

        // IP identifiers are compared by address, so any valid spelling of the address matches.
        private static bool IdentifierValueEquals(IdentifierType type, string actual, string expected)
            => type == IdentifierType.Ip &&
                IpAddressUtil.TryParse(actual, out var actualIp) &&
                IpAddressUtil.TryParse(expected, out var expectedIp)
                ? actualIp.Equals(expectedIp)
                : string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }
}

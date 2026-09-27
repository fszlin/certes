using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;

namespace Certes
{
    /// <summary>
    /// Extension methods for <see cref="IAcmeContext"/>.
    /// </summary>
    public static class IAcmeContextExtensions
    {
        /// <summary>
        /// Gets a resource URI.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="getter">The getter to retrieve resource URI from <see cref="Directory"/>.</param>
        /// <param name="optional">if set to <c>true</c>, the resource is optional.</param>
        /// <returns>The resource URI, or <c>null</c> if not found</returns>
        /// <exception cref="NotSupportedException">If the ACME operation not supported.</exception>
        internal static async Task<Uri> GetResourceUri(this IAcmeContext context, Func<Directory, Uri> getter, bool optional = false)
        {
            var dir = await context.GetDirectory();
            var uri = getter(dir);
            if (!optional && uri == null)
            {
                throw new NotSupportedException("ACME operation not supported.");
            }

            return uri;
        }

        /// <summary>
        /// Creates an account.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="email">The email.</param>
        /// <param name="termsOfServiceAgreed">Set to <c>true</c> to accept the terms of service.</param>
        /// <param name="eabKeyId">Optional key identifier for external account binding</param>
        /// <param name="eabKey">Optional key for use with external account binding</param>
        /// <param name="eabKeyAlg">Optional key algorithm e.g HS256, for external account binding</param>
        /// <returns>
        /// The account created.
        /// </returns>
        public static Task<IAccountContext> NewAccount(this IAcmeContext context, string email, bool termsOfServiceAgreed = false, string eabKeyId = null, string eabKey = null, string eabKeyAlg = null)
            => context.NewAccount(new[] { $"mailto:{email}" }, termsOfServiceAgreed, eabKeyId, eabKey, eabKeyAlg);

        /// <summary>
        /// Gets the terms of service link from the ACME server.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <returns>The terms of service link.</returns>
        public static async Task<Uri> TermsOfService(this IAcmeContext context)
        {
            var dir = await context.GetDirectory();
            return dir.Meta?.TermsOfService;
        }

        /// <summary>
        /// Gets the ACME Renewal Information (ARI) of a certificate, as defined in RFC 9773.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="certificateId">
        /// The ARI certificate identifier, from
        /// <see cref="RenewalInfoExtensions.GetRenewalInfoCertificateId(CertificateChain)"/>.
        /// </param>
        /// <returns>
        /// The renewal information, including the suggested renewal window and the
        /// server-requested <see cref="RenewalInfo.RetryAfter"/> delay before polling again.
        /// </returns>
        /// <exception cref="ArgumentException">If <paramref name="certificateId"/> is empty or malformed.</exception>
        /// <exception cref="NotSupportedException">If the server does not advertise a renewal information endpoint.</exception>
        /// <exception cref="AcmeRequestException">If the server returns an error.</exception>
        public static async Task<RenewalInfo> GetRenewalInfo(this IAcmeContext context, string certificateId)
        {
            if (string.IsNullOrWhiteSpace(certificateId) || certificateId.IndexOfAny(new[] { '/', '?', '#' }) >= 0)
            {
                throw new ArgumentException("Invalid ARI certificate identifier.", nameof(certificateId));
            }

            var endpoint = await context.GetResourceUri(d => d.RenewalInfo);
            var uri = new Uri($"{endpoint.AbsoluteUri.TrimEnd('/')}/{certificateId}");

            var resp = await context.HttpClient.Get<RenewalInfo>(uri);
            if (resp.Error != null)
            {
                throw new AcmeRequestException(
                    string.Format(Properties.Strings.ErrorFetchResource, uri),
                    resp.Error);
            }

            var info = resp.Resource ?? throw new AcmeException(
                string.Format(Properties.Strings.ErrorFetchResource, uri));

            info.RetryAfter = resp.RetryAfter > 0 ? TimeSpan.FromSeconds(resp.RetryAfter) : (TimeSpan?)null;
            return info;
        }

        /// <summary>
        /// Creates a new order that replaces a previously issued certificate, as defined in RFC 9773.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="identifiers">The identifiers.</param>
        /// <param name="replacedCertificateId">
        /// The ARI certificate identifier of the certificate being replaced, from
        /// <see cref="RenewalInfoExtensions.GetRenewalInfoCertificateId(CertificateChain)"/>.
        /// </param>
        /// <param name="notBefore">The value of not before field for the certificate.</param>
        /// <param name="notAfter">The value of not after field for the certificate.</param>
        /// <returns>
        /// The order context created.
        /// </returns>
        /// <exception cref="ArgumentException">If <paramref name="replacedCertificateId"/> is empty.</exception>
        public static async Task<IOrderContext> NewReplacementOrder(
            this IAcmeContext context,
            IList<string> identifiers,
            string replacedCertificateId,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? notAfter = null)
        {
            if (string.IsNullOrWhiteSpace(replacedCertificateId))
            {
                throw new ArgumentException("Invalid ARI certificate identifier.", nameof(replacedCertificateId));
            }

            var endpoint = await context.GetResourceUri(d => d.NewOrder);

            var body = new Order
            {
                Identifiers = identifiers
                    .Select(id => new Identifier { Type = IdentifierType.Dns, Value = id })
                    .ToArray(),
                NotBefore = notBefore,
                NotAfter = notAfter,
                Replaces = replacedCertificateId,
            };

            var order = await context.HttpClient.Post<Order>(context, endpoint, body, true);
            return new OrderContext(context, order.Location);
        }
    }
}

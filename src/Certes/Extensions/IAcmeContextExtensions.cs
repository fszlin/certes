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
        /// <exception cref="AcmeException">If the response or suggested window is missing, or the window ends at or before it starts.</exception>
        public static async Task<RenewalInfo> GetRenewalInfo(this IAcmeContext context, string certificateId)
        {
            ValidateCertificateId(certificateId, nameof(certificateId));

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

            if (info.SuggestedWindow == null || info.SuggestedWindow.End <= info.SuggestedWindow.Start)
            {
                throw new AcmeException("The renewal information response contains a missing or invalid suggested window.");
            }

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
        /// <exception cref="ArgumentException">If <paramref name="replacedCertificateId"/> is empty or malformed.</exception>
        public static async Task<IOrderContext> NewReplacementOrder(
            this IAcmeContext context,
            IList<string> identifiers,
            string replacedCertificateId,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? notAfter = null)
        {
            ValidateCertificateId(replacedCertificateId, nameof(replacedCertificateId));

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

        /// <summary>
        /// Creates an order using an advertised certificate profile.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="identifiers">The DNS identifiers.</param>
        /// <param name="profile">The exact, case-sensitive profile name advertised in directory metadata.</param>
        /// <param name="notBefore">The requested certificate validity start.</param>
        /// <param name="notAfter">The requested certificate validity end.</param>
        /// <param name="replacedCertificateId">Optional ARI identifier of the certificate being replaced.</param>
        /// <returns>The created order context.</returns>
        /// <exception cref="ArgumentException">The profile is empty or not advertised, or the replacement ID is malformed.</exception>
        /// <exception cref="NotSupportedException">The server does not advertise profiles or a new-order endpoint.</exception>
        /// <exception cref="AcmeRequestException">The server rejects the order, for example with invalidProfile.</exception>
        public static async Task<IOrderContext> NewOrderWithProfile(
            this IAcmeContext context,
            IList<string> identifiers,
            string profile,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? notAfter = null,
            string replacedCertificateId = null)
        {
            if (string.IsNullOrWhiteSpace(profile))
            {
                throw new ArgumentException("A certificate profile is required.", nameof(profile));
            }

            if (replacedCertificateId != null)
            {
                ValidateCertificateId(replacedCertificateId, nameof(replacedCertificateId));
            }

            var directory = await context.GetDirectory();
            var profiles = directory.Meta?.Profiles;
            if (profiles == null || profiles.Count == 0 || directory.NewOrder == null)
            {
                throw new NotSupportedException("Certificate profile selection is not supported by this server.");
            }

            if (!profiles.Keys.Contains(profile, StringComparer.Ordinal))
            {
                throw new ArgumentException("The certificate profile is not advertised by this server.", nameof(profile));
            }

            var body = new Order
            {
                Identifiers = identifiers
                    .Select(id => new Identifier { Type = IdentifierType.Dns, Value = id })
                    .ToArray(),
                Profile = profile,
                NotBefore = notBefore,
                NotAfter = notAfter,
                Replaces = replacedCertificateId,
            };

            var order = await context.HttpClient.Post<Order>(context, directory.NewOrder, body, true);
            return new OrderContext(context, order.Location);
        }

        private static void ValidateCertificateId(string certificateId, string paramName)
        {
            if (string.IsNullOrWhiteSpace(certificateId) || certificateId.IndexOfAny(new[] { '/', '?', '#' }) >= 0)
            {
                throw new ArgumentException("Invalid ARI certificate identifier.", paramName);
            }
        }
    }
}

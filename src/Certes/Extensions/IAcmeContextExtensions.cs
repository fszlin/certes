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
        /// <param name="identifiers">
        /// The identifiers. Values that parse as IP addresses are sent as IP identifiers; others as DNS.
        /// </param>
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
        public static Task<IOrderContext> NewReplacementOrder(
            this IAcmeContext context,
            IList<string> identifiers,
            string replacedCertificateId,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? notAfter = null)
            => context.NewReplacementOrder(ToIdentifiers(identifiers), replacedCertificateId, notBefore, notAfter);

        /// <summary>
        /// Creates a new order with explicitly typed identifiers (for example, IP addresses per RFC 8738)
        /// that replaces a previously issued certificate, as defined in RFC 9773.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="identifiers">The DNS or IP identifiers.</param>
        /// <param name="replacedCertificateId">The ARI certificate identifier of the certificate being replaced.</param>
        /// <param name="notBefore">The value of not before field for the certificate.</param>
        /// <param name="notAfter">The value of not after field for the certificate.</param>
        /// <returns>The order context created.</returns>
        /// <exception cref="ArgumentException">An identifier or <paramref name="replacedCertificateId"/> is empty or malformed.</exception>
        public static async Task<IOrderContext> NewReplacementOrder(
            this IAcmeContext context,
            IList<Identifier> identifiers,
            string replacedCertificateId,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? notAfter = null)
        {
            ValidateCertificateId(replacedCertificateId, nameof(replacedCertificateId));
            var body = CreateOrderBody(identifiers, notBefore, notAfter);
            body.Replaces = replacedCertificateId;

            var endpoint = await context.GetResourceUri(d => d.NewOrder);
            var order = await context.HttpClient.Post<Order>(context, endpoint, body, true);
            return new OrderContext(context, order.Location);
        }

        /// <summary>
        /// Creates a new order with explicitly typed identifiers, for example IP addresses (RFC 8738).
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="identifiers">The DNS or IP identifiers. IP values are normalized to their canonical text form.</param>
        /// <param name="notBefore">The value of not before field for the certificate.</param>
        /// <param name="notAfter">The value of not after field for the certificate.</param>
        /// <returns>The order context created.</returns>
        /// <exception cref="ArgumentException">An identifier is missing, empty, or not a valid IP address for type <see cref="IdentifierType.Ip"/>.</exception>
        public static async Task<IOrderContext> NewOrder(
            this IAcmeContext context,
            IList<Identifier> identifiers,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? notAfter = null)
        {
            var body = CreateOrderBody(identifiers, notBefore, notAfter);
            var endpoint = await context.GetResourceUri(d => d.NewOrder);
            var order = await context.HttpClient.Post<Order>(context, endpoint, body, true);
            return new OrderContext(context, order.Location);
        }

        /// <summary>
        /// Creates an order using an advertised certificate profile.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="identifiers">
        /// The identifiers. Values that parse as IP addresses are sent as IP identifiers; others as DNS.
        /// </param>
        /// <param name="profile">The exact, case-sensitive profile name advertised in directory metadata.</param>
        /// <param name="notBefore">The requested certificate validity start.</param>
        /// <param name="notAfter">The requested certificate validity end.</param>
        /// <param name="replacedCertificateId">Optional ARI identifier of the certificate being replaced.</param>
        /// <param name="allowUnadvertisedProfile">
        /// Set to <c>true</c> to send a profile name that the directory does not list, such as a
        /// private profile agreed with the CA or a deprecated profile during replacement. The server
        /// must still advertise profile support. Defaults to <c>false</c>.
        /// </param>
        /// <returns>The created order context.</returns>
        /// <exception cref="ArgumentException">The profile is empty or not advertised, or the replacement ID is malformed.</exception>
        /// <exception cref="NotSupportedException">The server does not advertise profiles or a new-order endpoint.</exception>
        /// <exception cref="AcmeRequestException">The server rejects the order, for example with invalidProfile.</exception>
        public static Task<IOrderContext> NewOrderWithProfile(
            this IAcmeContext context,
            IList<string> identifiers,
            string profile,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? notAfter = null,
            string replacedCertificateId = null,
            bool allowUnadvertisedProfile = false)
            => context.NewOrderWithProfile(ToIdentifiers(identifiers), profile, notBefore, notAfter, replacedCertificateId, allowUnadvertisedProfile);

        /// <summary>
        /// Creates an order with explicitly typed identifiers using an advertised certificate profile.
        /// Some CAs, such as Let's Encrypt, issue IP address certificates only under specific profiles.
        /// </summary>
        /// <param name="context">The ACME context.</param>
        /// <param name="identifiers">The DNS or IP identifiers.</param>
        /// <param name="profile">The exact, case-sensitive profile name advertised in directory metadata.</param>
        /// <param name="notBefore">The requested certificate validity start.</param>
        /// <param name="notAfter">The requested certificate validity end.</param>
        /// <param name="replacedCertificateId">Optional ARI identifier of the certificate being replaced.</param>
        /// <param name="allowUnadvertisedProfile">Set to <c>true</c> to send a profile name that the directory does not list.</param>
        /// <returns>The created order context.</returns>
        /// <exception cref="ArgumentException">The profile, an identifier, or the replacement ID is empty or malformed, or the profile is not advertised.</exception>
        /// <exception cref="NotSupportedException">The server does not advertise profiles or a new-order endpoint.</exception>
        /// <exception cref="AcmeRequestException">The server rejects the order, for example with invalidProfile.</exception>
        public static async Task<IOrderContext> NewOrderWithProfile(
            this IAcmeContext context,
            IList<Identifier> identifiers,
            string profile,
            DateTimeOffset? notBefore = null,
            DateTimeOffset? notAfter = null,
            string replacedCertificateId = null,
            bool allowUnadvertisedProfile = false)
        {
            if (string.IsNullOrWhiteSpace(profile))
            {
                throw new ArgumentException("A certificate profile is required.", nameof(profile));
            }

            if (replacedCertificateId != null)
            {
                ValidateCertificateId(replacedCertificateId, nameof(replacedCertificateId));
            }

            var body = CreateOrderBody(identifiers, notBefore, notAfter);
            body.Profile = profile;
            body.Replaces = replacedCertificateId;

            var directory = await context.GetDirectory();
            var profiles = directory.Meta?.Profiles;
            if (profiles == null || profiles.Count == 0 || directory.NewOrder == null)
            {
                throw new NotSupportedException("Certificate profile selection is not supported by this server.");
            }

            if (!allowUnadvertisedProfile && !profiles.ContainsKey(profile))
            {
                throw new ArgumentException("The certificate profile is not advertised by this server.", nameof(profile));
            }

            var order = await context.HttpClient.Post<Order>(context, directory.NewOrder, body, true);
            return new OrderContext(context, order.Location);
        }

        /// <summary>
        /// Builds a new-order request body from identifier strings (IP addresses detected) and optional validity bounds.
        /// </summary>
        internal static Order CreateOrderBody(IList<string> identifiers, DateTimeOffset? notBefore, DateTimeOffset? notAfter)
            => new Order
            {
                Identifiers = ToIdentifiers(identifiers),
                NotBefore = notBefore,
                NotAfter = notAfter,
            };

        /// <summary>
        /// Builds a new-order request body from typed identifiers, validating and normalizing IP values.
        /// The caller's identifier objects are not modified.
        /// </summary>
        internal static Order CreateOrderBody(IList<Identifier> identifiers, DateTimeOffset? notBefore, DateTimeOffset? notAfter)
        {
            if (identifiers == null)
            {
                throw new ArgumentNullException(nameof(identifiers));
            }

            var copies = new Identifier[identifiers.Count];
            for (var i = 0; i < copies.Length; i++)
            {
                var id = identifiers[i];
                if (id == null || string.IsNullOrWhiteSpace(id.Value))
                {
                    throw new ArgumentException("Identifiers must have a value.", nameof(identifiers));
                }

                var value = id.Value;
                if (id.Type == IdentifierType.Ip && !IpAddressUtil.TryNormalize(value, out value))
                {
                    throw new ArgumentException($"'{id.Value}' is not a valid IP address identifier.", nameof(identifiers));
                }

                copies[i] = new Identifier { Type = id.Type, Value = value };
            }

            return new Order
            {
                Identifiers = copies,
                NotBefore = notBefore,
                NotAfter = notAfter,
            };
        }

        /// <summary>
        /// Converts plain identifier strings: values that strictly parse as IP addresses become
        /// canonical <see cref="IdentifierType.Ip"/> identifiers (RFC 8738); all others are DNS.
        /// </summary>
        internal static Identifier[] ToIdentifiers(IList<string> identifiers)
            => identifiers
                .Select(id => IpAddressUtil.TryNormalize(id, out var ip)
                    ? new Identifier { Type = IdentifierType.Ip, Value = ip }
                    : new Identifier { Type = IdentifierType.Dns, Value = id })
                .ToArray();

        private static void ValidateCertificateId(string certificateId, string paramName)
        {
            if (string.IsNullOrWhiteSpace(certificateId) || certificateId.IndexOfAny(new[] { '/', '?', '#' }) >= 0)
            {
                throw new ArgumentException("Invalid ARI certificate identifier.", paramName);
            }
        }
    }
}

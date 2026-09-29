using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Jws;
using Org.BouncyCastle.Security;

namespace Certes
{
    /// <summary>
    /// Interactive DNS persistent authorization helpers for draft-ietf-acme-dns-persist-02.
    /// </summary>
    public static class DnsPersistExtensions
    {
        /// <summary>
        /// Builds a domain-bound hashed-account TXT record for an offered dns-persist-01 challenge.
        /// The caller publishes the record, then calls <see cref="IChallengeContext.Validate"/>.
        /// </summary>
        /// <param name="context">The context whose current account key will authorize issuance.</param>
        /// <param name="authorization">The authorization containing the DNS identifier.</param>
        /// <param name="challenge">The offered challenge resource.</param>
        /// <param name="wildcardPolicy">Explicitly authorize the base domain, wildcards and subdomains.</param>
        /// <param name="persistUntil">Optional expiration of the persistent authorization record.</param>
        /// <param name="issuerDomainName">An advertised issuer identity; defaults to the first.</param>
        /// <param name="cancellationToken">Cancels directory and account discovery.</param>
        /// <returns>The record owner, value and zone-file representation.</returns>
        /// <remarks>
        /// This implements the -02 draft only; older cleartext-account drafts are not supported.
        /// It does not publish DNS records or implement delegated pre-provisioning.
        /// </remarks>
        public static async Task<DnsPersistRecord> GetDnsPersistRecord(
            this IAcmeContext context,
            Authorization authorization,
            Challenge challenge,
            bool wildcardPolicy = false,
            DateTimeOffset? persistUntil = null,
            string issuerDomainName = null,
            CancellationToken cancellationToken = default)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (authorization == null)
            {
                throw new ArgumentNullException(nameof(authorization));
            }

            if (challenge == null)
            {
                throw new ArgumentNullException(nameof(challenge));
            }

            cancellationToken.ThrowIfCancellationRequested();
            var directory = await context.GetDirectory(cancellationToken);
            ValidateChallenge(challenge, directory.Meta);
            var account = await context.Account(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return CreateRecord(authorization, challenge, directory.Meta, account.Location,
                context.AccountKey.Thumbprint(), wildcardPolicy, persistUntil, issuerDomainName);
        }

        internal static DnsPersistRecord CreateRecord(
            Authorization authorization, Challenge challenge, DirectoryMeta meta,
            Uri accountUrl, string thumbprint, bool wildcardPolicy = false,
            DateTimeOffset? persistUntil = null, string issuerDomainName = null)
        {
            if (authorization == null)
            {
                throw new ArgumentNullException(nameof(authorization));
            }

            if (challenge == null)
            {
                throw new ArgumentNullException(nameof(challenge));
            }

            if (authorization.Identifier?.Type != IdentifierType.Dns)
            {
                throw new ArgumentException("DNS persistent authorization requires a DNS identifier.", nameof(authorization));
            }

            var domain = NormalizeDomain(authorization.Identifier.Value);
            var name = "_validation-persist." + domain;
            if (name.Length > 253)
            {
                throw new ArgumentException("The persistent DNS record owner exceeds the DNS name length limit.", nameof(authorization));
            }

            if (authorization.Wildcard == true && !wildcardPolicy)
            {
                throw new ArgumentException("Wildcard authorization requires explicit wildcardPolicy consent.", nameof(wildcardPolicy));
            }

            if (persistUntil.HasValue && persistUntil.Value <= DateTimeOffset.UtcNow)
            {
                throw new ArgumentOutOfRangeException(nameof(persistUntil), "The persistent authorization expiration must be in the future.");
            }

            ValidateChallenge(challenge, meta);
            var issuer = issuerDomainName ?? challenge.IssuerDomainNames[0];
            if (!challenge.IssuerDomainNames.Contains(issuer, StringComparer.Ordinal))
            {
                throw new ArgumentException("The issuer identity must be advertised by the challenge.", nameof(issuerDomainName));
            }

            // Preserve the exact prefix and account URL: -02 requires simple string comparison.
            var prefix = meta?.AccountHashPrefix;
            if (accountUrl == null || !accountUrl.IsAbsoluteUri ||
                accountUrl.OriginalString.Any(c => c < '!' || c > '~') ||
                !Uri.IsWellFormedUriString(accountUrl.OriginalString, UriKind.Absolute))
            {
                throw new ArgumentException("An absolute ASCII account URL is required.", nameof(accountUrl));
            }

            if (thumbprint == null || thumbprint.Length != 43 || thumbprint.Any(c =>
                !(c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-' || c == '_')))
            {
                throw new ArgumentException("A base64url SHA-256 JWK thumbprint is required.", nameof(thumbprint));
            }

            var inputs = Encoding.ASCII.GetBytes(domain + thumbprint + accountUrl.OriginalString);
            var bytes = new byte[inputs.Length + 1];
            bytes[0] = (byte)domain.Length;
            Buffer.BlockCopy(inputs, 0, bytes, 1, inputs.Length);
            var hash = JwsConvert.ToBase64String(DigestUtilities.CalculateDigest("SHA256", bytes));
            var value = issuer + "; accounturi=" + prefix + "sha-256/" + hash;
            if (wildcardPolicy)
            {
                value += "; policy=wildcard";
            }

            if (persistUntil.HasValue)
            {
                value += "; persistUntil=" + persistUntil.Value.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
            }

            return new DnsPersistRecord(name, value);
        }

        internal static void ValidateChallenge(Challenge challenge, DirectoryMeta meta)
        {
            if (challenge == null || challenge.Type != ChallengeTypes.DnsPersist01 || !ValidIssuerNames(challenge.IssuerDomainNames))
            {
                throw new AcmeException("Malformed dns-persist-01 challenge: expected 1 to 10 normalized issuerDomainNames (draft -02).");
            }

            var prefix = meta?.AccountHashPrefix;
            if (!IsParameterValue(prefix) || !Uri.IsWellFormedUriString(prefix + "sha-256/test", UriKind.Absolute))
            {
                throw new AcmeException("The server does not advertise a valid accountHashPrefix required by dns-persist-01 draft -02.");
            }

            // Missing/nonconforming directory identities are unavailable, not a prohibition
            // on interactive challenges. Available identities impose the draft's subset checks.
            if (ValidIssuerNames(meta?.IssuerDomainNames) &&
                meta.IssuerDomainNames.Any(n => !challenge.IssuerDomainNames.Contains(n, StringComparer.Ordinal)))
            {
                throw new AcmeException("The dns-persist-01 challenge omits a directory issuer identity.");
            }

            var caa = meta?.CaaIdentities;
            if (caa != null && caa.Count > 0)
            {
                string[] normalized;
                try
                {
                    normalized = caa.Select(NormalizeDomain).ToArray();
                }
                catch (ArgumentException)
                {
                    return;
                }

                if (challenge.IssuerDomainNames.Any(n => !normalized.Contains(n, StringComparer.Ordinal)))
                {
                    throw new AcmeException("The dns-persist-01 challenge contains an issuer identity absent from caaIdentities.");
                }
            }
        }

        private static bool ValidIssuerNames(IList<string> names)
        {
            if (names == null || names.Count == 0 || names.Count > 10)
            {
                return false;
            }

            try
            {
                return names.All(n => NormalizeDomain(n) == n);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static string NormalizeDomain(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("A DNS name is required.");
            }

            var domain = new IdnMapping { UseStd3AsciiRules = true }.GetAscii(value.Normalize().ToLowerInvariant());
            if (domain.EndsWith(".", StringComparison.Ordinal))
            {
                domain = domain.Substring(0, domain.Length - 1);
            }

            if (domain.Length == 0 || domain.Length > 253 || IpAddressUtil.TryParse(domain, out _) ||
                domain.Split('.').Any(label => label.Length == 0 || label.Length > 63 ||
                    label[0] == '-' || label[label.Length - 1] == '-' ||
                    label.Any(c => !(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '-'))))
            {
                throw new ArgumentException("A valid DNS name in A-label form is required.");
            }

            return domain;
        }

        private static bool IsParameterValue(string value) =>
            !string.IsNullOrEmpty(value) && value.All(c => c >= '!' && c <= '~' && c != ';');
    }
}

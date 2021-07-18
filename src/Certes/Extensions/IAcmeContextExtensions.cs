using System;
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
    }
}

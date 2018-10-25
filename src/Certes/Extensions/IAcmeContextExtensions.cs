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
        /// <returns>
        /// The account created.
        /// </returns>
        public static Task<IAccountContext> NewAccount(this IAcmeContext context, string email, bool termsOfServiceAgreed = false)
            => context.NewAccount(new[] { $"mailto:{email}" }, termsOfServiceAgreed);

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

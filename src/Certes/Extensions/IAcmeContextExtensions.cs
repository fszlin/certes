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
        /// News the account.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="email">The email.</param>
        /// <param name="termsOfServiceAgreed">if set to <c>true</c> [terms of service agreed].</param>
        /// <returns></returns>
        public static Task<IAccountContext> NewAccount(this IAcmeContext context, string email, bool termsOfServiceAgreed = false)
            => context.NewAccount(new[] { $"mailto:{email}" }, termsOfServiceAgreed);
    }
}

using System;
using System.Threading.Tasks;
using Certes.Acme.Resource;

namespace Certes
{
    internal static class IAcmeContextExtensions
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
    }
}

using System.Threading.Tasks;
using Certes.Acme.Resource;
using Certes.Crypto;
using Certes.Pkcs;

namespace Certes.Acme
{
    /// <summary>
    /// Extension methods for <see cref="IOrderContext"/>.
    /// </summary>
    public static class IOrderContextExtensions
    {
        /// <summary>
        /// Finalizes the specified CSR.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="csr">The CSR.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static async Task<Order> Finalize(this IOrderContext context, CsrInfo csr, ISignatureKey key)
        {
            var builder = new CertificationRequestBuilder(key);
            foreach (var authzCtx in await context.Authorizations())
            {
                var authz = await authzCtx.Resource();
                builder.SubjectAlternativeNames.Add(authz.Identifier.Value);
            }

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
        /// Generates the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="csr">The CSR.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static async Task<CertificateInfo> Generate(this IOrderContext context, CsrInfo csr, ISignatureKey key = null)
        {
            if (key == null)
            {
                key = DSA.NewKey(SignatureAlgorithm.RS256);
            }

            await context.Finalize(csr, key);
            var pem = await context.Download();

            return new CertificateInfo(pem, key);
        }
    }
}

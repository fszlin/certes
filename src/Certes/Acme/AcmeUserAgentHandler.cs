using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Certes.Acme
{
    internal class AcmeUserAgentHandler : DelegatingHandler
    {
        static readonly ProductInfoHeaderValue certesVersionUserAgentHeader = new ProductInfoHeaderValue("Certes", typeof(AcmeUserAgentHandler).GetTypeInfo().Assembly.GetName().Version.ToString());
        static readonly ProductInfoHeaderValue acmeVersionUserAgentHeader = new ProductInfoHeaderValue("(ACME 2.0)");

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.Add(certesVersionUserAgentHeader);
            request.Headers.UserAgent.Add(acmeVersionUserAgentHeader);

            return base.SendAsync(request, cancellationToken);
        }
    }
}

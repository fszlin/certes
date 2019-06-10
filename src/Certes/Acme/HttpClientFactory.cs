using System.Net.Http;

namespace Certes.Acme
{
    internal static class HttpClientFactory
    {
        public static HttpClient CreateInternalHttpClient(HttpMessageHandler httpMessageHandler = null)
        {
            var handler = new AcmeUserAgentHandler();

            if(httpMessageHandler!=null)
            {
                handler.InnerHandler = httpMessageHandler;
            }
            else
            {
                handler.InnerHandler = new HttpClientHandler();
            }

            return new HttpClient(handler, true);
        }
    }
}

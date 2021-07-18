using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;

namespace Certes.Tests.CI
{
    public static class CertificateFunction
    {
        private static string certificateData { get; set; }
        private static string certificateKey { get; set; }

        [Function("CertData")]
        public static async Task<HttpResponseData> CertData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cert-data")] HttpRequestData req,
            FunctionContext executionContext)
        {
            if (certificateData == null)
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("request");
                var json = await http.GetStringAsync("https://api.github.com/repos/fszlin/lo0.in/releases/latest");
                var metadata = JObject.Parse(json);

                var certUrl = metadata["assets"]
                    .AsJEnumerable()
                    .Where(a => a["name"].Value<string>() == "cert.pem")
                    .Select(a => a["browser_download_url"].Value<string>())
                    .First();

                certificateData = await http.GetStringAsync(certUrl);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(certificateData);
            return response;
        }

        [Function("CertKey")]
        public static async Task<HttpResponseData> CertKey(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cert-key")] HttpRequestData req,
            FunctionContext executionContext)
        {
            if (certificateKey == null)
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("request");
                var json = await http.GetStringAsync("https://api.github.com/repos/fszlin/lo0.in/releases/latest");
                var metadata = JObject.Parse(json);

                var certUrl = metadata["assets"]
                    .AsJEnumerable()
                    .Where(a => a["name"].Value<string>() == "key.pem")
                    .Select(a => a["browser_download_url"].Value<string>())
                    .First();

                certificateKey = await http.GetStringAsync(certUrl);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(certificateKey);
            return response;
        }
    }
}

using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;

namespace Certes.Tests.CI
{
    public static class CertificateFunction
    {
        private static string certificateData { get; set; }
        private static string certificateKey { get; set; }

        [FunctionName("CertData")]
        public static async Task<IActionResult> CertData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cert-data")]HttpRequest request,
            TraceWriter log)
        {
            if (certificateData == null)
            {
                using (var http = new HttpClient())
                {
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
            }

            return new ContentResult
            {
                Content = certificateData,
                ContentType = "application/x-pem-file",
                StatusCode = 200,
            };
        }

        [FunctionName("CertKey")]
        public static async Task<IActionResult> CertKey(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cert-key")]HttpRequest request,
            TraceWriter log)
        {
            if (certificateKey == null)
            {
                using (var http = new HttpClient())
                {
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
            }

            return new ContentResult
            {
                Content = certificateKey,
                ContentType = "application/x-pem-file",
                StatusCode = 200,
            };
        }
    }
}

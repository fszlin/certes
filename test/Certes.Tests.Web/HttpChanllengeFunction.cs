using System.Net;
using System.Threading.Tasks;
using Certes.Jws;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using static Certes.Tests.CI.Helper;

namespace Certes.Tests.CI
{
    public static class HttpChanllengeFunction
    {
        [Function("ResponseHttp")]
        public static async Task<HttpResponseData> ResponseHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/acme-challenge/{*token}")] HttpRequestData req,
            string token,
            FunctionContext executionContext)
        {
            var accountKey = GetTestKey(req);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(accountKey.KeyAuthorization(token));
            return response;
        }
    }
}

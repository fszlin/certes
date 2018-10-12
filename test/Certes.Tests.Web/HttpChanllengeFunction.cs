using Certes.Jws;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using static Certes.Tests.CI.Helper;

namespace Certes.Tests.CI
{
    public static class HttpChanllengeFunction
    {
        [FunctionName("ResponseHttp")]
        public static IActionResult ResponseHttp(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/acme-challenge/{*token}")]HttpRequest request,
            string token,
            ILogger log)
        {
            var accountKey = GetTestKey(request);

            return new ContentResult
            {
                Content = accountKey.KeyAuthorization(token),
                ContentType = "plain/text",
                StatusCode = 200,
            };
        }
    }
}

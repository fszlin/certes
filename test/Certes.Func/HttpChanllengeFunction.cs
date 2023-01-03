using System.Net;
using Certes.Jws;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using static Certes.Func.Helper;

namespace Certes.Func
{
    public class HttpChanllengeFunction
    {
        private readonly ILogger _logger;

        public HttpChanllengeFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<HttpChanllengeFunction>();
        }

        [Function(nameof(HandleHttpChallenge))]
        public async Task<HttpResponseData> HandleHttpChallenge(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = ".well-known/acme-challenge/{*token}")] HttpRequestData req,
            string token)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var accountKey = GetTestKey(req);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(accountKey.KeyAuthorization(token));
            return response;
        }
    }
}

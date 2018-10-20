using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using static Certes.Tests.CI.Helper;

namespace Certes.Tests.CI
{
    public static class DnsChanllengeFunction
    {
        [FunctionName("SetupDns")]
        public static async Task<IActionResult> SetupDns(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "dns-01/{algo}")]HttpRequest request,
            string algo,
            ILogger log)
        {
            Dictionary<string, string> tokens;
            using (var reader = new StreamReader(request.Body))
            {
                var json = await reader.ReadToEndAsync();
                tokens = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            }

            var keyType = (KeyAlgorithm)Enum.Parse(typeof(KeyAlgorithm), algo, true);
            var accountKey = GetTestKey(keyType);

            var loginInfo = new ServicePrincipalLoginInformation
            {
                ClientId = Env("CERTES_AZURE_CLIENT_ID"),
                ClientSecret = Env("CERTES_AZURE_CLIENT_SECRET"),
            };

            var credentials = new AzureCredentials(loginInfo, Env("CERTES_AZURE_TENANT_ID"), AzureEnvironment.AzureGlobalCloud);
            var builder = RestClient.Configure();
            var resClient = builder.WithEnvironment(AzureEnvironment.AzureGlobalCloud)
                .WithCredentials(credentials)
                .Build();
            using (var client = new DnsManagementClient(resClient))
            {
                client.SubscriptionId = Env("CERTES_AZURE_SUBSCRIPTION_ID");

                foreach (var p in tokens)
                {
                    var name = "_acme-challenge." + p.Key.Replace(".dymetis.com", "");
                    await client.RecordSets.CreateOrUpdateAsync(
                        "dymetis",
                        "dymetis.com",
                        name,
                        RecordType.TXT,
                        new RecordSetInner(
                            name: name,
                            tTL: 1,
                            txtRecords: new[] { new TxtRecord(new[] { accountKey.SignatureKey.DnsTxt(p.Value) }) }));
                }
            }

            return new OkResult();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Certes.Acme;
using Newtonsoft.Json;

namespace Certes
{
    public class StagingServers
    {
        private static Uri[] StagingServersV1 = new[]
        {
            new Uri("http://localhost:4000/directory"),
            new Uri("http://boulder-certes-ci.dymetis.com:4000/directory"),
            WellKnownServers.LetsEncryptStaging,
        };

        private static readonly Lazy<HttpClient> http = new Lazy<HttpClient>(() => new HttpClient());
        private static Uri stagingServerV1;
        private static Uri stagingServerV2;


        public static async Task<Uri> GetUriV1()
        {
            if (stagingServerV1 != null)
            {
                return stagingServerV1;
            }

            var key = await Helper.LoadkeyV1();
            foreach (var uri in StagingServersV1)
            {
                var httpSucceed = false;
                try
                {
                    await http.Value.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                    httpSucceed = true;
                }
                catch
                {
                }

                if (httpSucceed)
                {
                    using (var client = new AcmeClient(uri))
                    {
                        client.Use(key.Export());

                        try
                        {
                            var account = await client.NewRegistraton();
                            account.Data.Agreement = account.GetTermsOfServiceUri();
                            await client.UpdateRegistration(account);
                        }
                        catch
                        {
                            // account already exists
                        }

                        return stagingServerV1 = uri;
                    }
                }
            }

            throw new Exception("Staging server unavailable.");
        }

        public static async Task<Uri> GetUriV2()
        {
            if (stagingServerV2 != null)
            {
                return stagingServerV2;
            }

            var servers = new[] {
                new Uri("http://localhost:4001/directory"),
                new Uri("http://boulder-certes-ci.dymetis.com:4001/directory"),
                WellKnownServers.LetsEncryptStagingV2,
            };

            using (var http = new HttpClient())
            {
                foreach (var uri in servers)
                {
                    try
                    {
                        await http.GetStringAsync(uri);

                        foreach (var algo in Enum.GetValues(typeof(KeyAlgorithm)).OfType<KeyAlgorithm>())
                        {
                            try
                            {
                                var ctx = new AcmeContext(uri, Helper.GetKeyV2(algo));
                                await ctx.NewAccount(new[] { "mailto:fszlin@example.com" }, true);
                            }
                            catch
                            {
                            }
                        }

                        return stagingServerV2 = uri;
                    }
                    catch
                    {
                    }
                }
            }

            throw new Exception("No staging server available.");
        }

        public static async Task DeployDns01(KeyAlgorithm algo, Dictionary<string, string> tokens)
        {
            using (await http.Value.PutAsync($"http://certes-ci.dymetis.com/dns-01/{algo}", new StringContent(JsonConvert.SerializeObject(tokens)))) { }
        }
    }
}

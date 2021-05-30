using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Pkcs;
using Newtonsoft.Json;

namespace Certes
{
    public static class IntegrationHelper
    {
        public static readonly byte[][] TestCertificates =
            new[] {
                File.ReadAllBytes("./Data/test-root.pem"),
                File.ReadAllBytes("./Data/root-cert-ecdsa.pem"),
                File.ReadAllBytes("./Data/root-cert-rsa.pem"),
            };

        private static Uri[] StagingServersV1 = new[]
        {
            //new Uri("https://lo0.in:4430/directory"),
            new Uri("https://boulder-certes-ci.dymetis.com:4430/directory"),
            //WellKnownServers.LetsEncryptStaging,
        };

        public static readonly Lazy<HttpClient> http = new Lazy<HttpClient>(() =>
        {
#if NETCOREAPP3_1_OR_GREATER
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator };
#elif NETCOREAPP1_0_OR_GREATER
            var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (msg, cert, chains, errors) => true };
#else
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (msg, cert, chains, errors) => true;
            var handler = new HttpClientHandler();
#endif

            return new HttpClient(handler);
        });

        private static Uri stagingServerV1;
        private static Uri stagingServerV2;

        public static IAcmeHttpClient GetAcmeHttpClient(Uri uri) => Helper.CreateHttp(uri, http.Value);

        public static IAcmeHttpHandler GetAcmeHttpHandler(Uri uri) => new AcmeHttpHandler(uri, http.Value);
        
        public static async Task<Uri> GetAcmeUriV1()
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
                    using (var client = new AcmeClient(new AcmeHttpHandler(uri, http.Value)))
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

        public static async Task<Uri> GetAcmeUriV2()
        {
            if (stagingServerV2 != null)
            {
                return stagingServerV2;
            }

            var servers = new[] {
                //new Uri("https://lo0.in:4431/directory"),
                new Uri("https://boulder-certes-ci.dymetis.com:4431/directory"),
                //WellKnownServers.LetsEncryptStagingV2,
            };

            var exceptions = new List<Exception>();
            foreach (var uri in servers)
            {
                try
                {
                    await http.Value.GetStringAsync(uri);

                    foreach (var algo in new[] { KeyAlgorithm.ES256, KeyAlgorithm.ES384, KeyAlgorithm.RS256 })
                    {
                        try
                        {
                            var ctx = new AcmeContext(uri, Helper.GetKeyV2(algo), GetAcmeHttpClient(uri));
                            await ctx.NewAccount(new[] { "mailto:ci@certes.app" }, true);
                        }
                        catch
                        {
                        }
                    }

                    return stagingServerV2 = uri;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            throw new AggregateException("No staging server available.", exceptions);
        }

        public static async Task DeployDns01(KeyAlgorithm algo, Dictionary<string, string> tokens)
        {
            using (await http.Value.PutAsync($"http://certes-ci.dymetis.com/dns-01/{algo}", new StringContent(JsonConvert.SerializeObject(tokens)))) { }
        }

        public static void AddTestCerts(this PfxBuilder pfx)
        {
            foreach (var cert in TestCertificates)
            {
                pfx.AddIssuers(cert);
            }
        }
    }
}

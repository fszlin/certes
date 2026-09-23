using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Pkcs;
using Newtonsoft.Json;
using Org.BouncyCastle.Pkcs;
using Xunit;

namespace Certes
{
    public static class IntegrationHelper
    {
        private static readonly Uri directory = new Uri("https://localhost:14000/dir");
        private static readonly Uri management = new Uri("https://localhost:15000/");
        private static readonly Uri challenges = new Uri("http://localhost:8055/");
        private static readonly Lazy<HttpClient> http = new Lazy<HttpClient>(CreateHttpClient);
        private static readonly Lazy<Task<Uri>> initialize = new Lazy<Task<Uri>>(Initialize);
        private static byte[] root;

        private static HttpClient CreateHttpClient()
        {
#if NET10_0_OR_GREATER
            var handler = new HttpClientHandler { AllowAutoRedirect = false, UseProxy = false };
            // Trust only this pinned test server certificate, for the two loopback TLS ports.
            // Never install Pebble's public test CA in the OS trust store.
            using var pinned = X509CertificateLoader.LoadCertificateFromFile(Path.Combine(AppContext.BaseDirectory, "Pebble", "localhost.pem"));
            var expected = pinned.RawData;
            handler.ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) =>
                request.RequestUri.Host == "localhost" &&
                (request.RequestUri.Port == 14000 || request.RequestUri.Port == 15000) &&
                certificate != null &&
                (errors & (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateNotAvailable)) == 0 &&
                DateTime.UtcNow >= certificate.NotBefore.ToUniversalTime() &&
                DateTime.UtcNow <= certificate.NotAfter.ToUniversalTime() &&
                expected.SequenceEqual(certificate.RawData);
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Certes-Integration/1.0");
            return client;
#else
            throw new PlatformNotSupportedException("The local Pebble integration harness runs on .NET 10. net462 is compile-only.");
#endif
        }

        public static IAcmeHttpClient GetAcmeHttpClient(Uri uri)
        {
            if (uri != directory)
            {
                throw new ArgumentException("Integration tests use only the local Pebble directory.", nameof(uri));
            }

            return new AcmeHttpClient(uri, http.Value);
        }

        public static Task<Uri> GetAcmeUriV2() => initialize.Value;

        private static async Task<Uri> Initialize()
        {
            var stage = "loading the pinned TLS certificate";
            try
            {
                var client = http.Value;
                stage = "fetching the ACME directory";
                await client.GetStringAsync(directory);
                stage = "fetching the issuance root from the management API";
                var fetchedRoot = await client.GetByteArrayAsync(new Uri(management, "roots/0"));
                foreach (var algorithm in new[] { KeyAlgorithm.RS256, KeyAlgorithm.ES256, KeyAlgorithm.ES384 })
                {
                    stage = $"provisioning the {algorithm} test account";
                    var context = new AcmeContext(directory, Helper.GetKeyV2(algorithm), GetAcmeHttpClient(directory));
                    await context.NewAccount(new[] { "mailto:ci@example.test" }, true);
                }

                root = fetchedRoot;
                return directory;
            }
#if NET10_0_OR_GREATER
            catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.SecureConnectionError)
            {
                throw new InvalidOperationException($"Pebble TLS validation failed while {stage}. Check the localhost certificate pin, hostname, validity dates, and pinned image version. See scripts/Pebble/README.md.", ex);
            }
            catch (HttpRequestException ex) when (ex.HttpRequestError == HttpRequestError.ConnectionError || ex.HttpRequestError == HttpRequestError.NameResolutionError)
            {
                throw new InvalidOperationException($"Could not connect to local Pebble while {stage}. Run docker compose -f scripts/Pebble/compose.yml up -d and bash scripts/Pebble/wait.sh; check loopback ports. See scripts/Pebble/README.md.", ex);
            }
#endif
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Pebble initialization failed while {stage}. Inspect the inner exception and container logs; this may be an HTTP, management API, or account-provisioning error. See scripts/Pebble/README.md.", ex);
            }
        }

        public static async Task ConfigureChallenge(string operation, object data)
        {
            using var content = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");
            using var response = await http.Value.PostAsync(new Uri(challenges, operation), content);
            response.EnsureSuccessStatusCode();
        }

        public static Task<IOrderContext> AuthorizeHttp(AcmeContext context, IList<string> hosts)
            => Authorize(context, hosts, ChallengeTypes.Http01);

        public static async Task<IOrderContext> Authorize(AcmeContext context, IList<string> hosts, string type)
        {
            var order = await context.NewOrder(hosts);
            var initial = await order.Resource();
            Assert.NotNull(initial);
            Assert.Equal(hosts.Count, initial.Authorizations?.Count);
            Assert.True(initial.Status == OrderStatus.Pending || initial.Status == OrderStatus.Ready,
                $"Unexpected initial order status: {initial.Status}");
            foreach (var authz in await order.Authorizations())
            {
                var resource = await authz.Resource();
                if (resource.Status == AuthorizationStatus.Valid)
                {
                    continue;
                }

                var challenge = await authz.Challenge(type);
                Assert.NotNull(challenge);
                var host = resource.Identifier.Value;
                var token = challenge.Token;
                var keyAuthz = context.AccountKey.KeyAuthorization(token);
                var dnsHost = $"_acme-challenge.{host}.";
                try
                {
                    if (type == ChallengeTypes.Http01)
                    {
                        await ConfigureChallenge("add-http01", new { token, content = keyAuthz });
                    }
                    else if (type == ChallengeTypes.Dns01)
                    {
                        await ConfigureChallenge("set-txt", new { host = dnsHost, value = context.AccountKey.DnsTxt(token) });
                    }
                    else if (type == ChallengeTypes.TlsAlpn01)
                    {
                        await ConfigureChallenge("add-tlsalpn01", new { host, content = keyAuthz });
                    }
                    else
                    {
                        throw new ArgumentException("Unsupported local challenge type.", nameof(type));
                    }

                    await challenge.Validate();
                    await WaitForAuthorization(authz);
                }
                finally
                {
                    if (type == ChallengeTypes.Http01)
                    {
                        await ConfigureChallenge("del-http01", new { token });
                    }
                    else if (type == ChallengeTypes.Dns01)
                    {
                        await ConfigureChallenge("clear-txt", new { host = dnsHost });
                    }
                    else if (type == ChallengeTypes.TlsAlpn01)
                    {
                        await ConfigureChallenge("del-tlsalpn01", new { host });
                    }
                }
            }

            await WaitForOrder(order, OrderStatus.Ready);
            return order;
        }

        public static async Task WaitForAuthorization(IAuthorizationContext authorization, AuthorizationStatus expected = AuthorizationStatus.Valid)
        {
            var clock = Stopwatch.StartNew();
            while (clock.Elapsed < TimeSpan.FromSeconds(60))
            {
                var resource = await authorization.Resource();
                if (resource.Status == expected)
                {
                    return;
                }

                if (resource.Status != AuthorizationStatus.Pending)
                {
                    throw new InvalidOperationException($"Authorization ended in {resource.Status}; expected {expected}.");
                }

                await Task.Delay(200);
            }

            throw new TimeoutException($"Authorization did not become {expected} within 60 seconds.");
        }

        public static async Task WaitForOrder(IOrderContext order, OrderStatus expected)
        {
            var clock = Stopwatch.StartNew();
            while (clock.Elapsed < TimeSpan.FromSeconds(60))
            {
                var resource = await order.Resource();
                if (resource.Status == expected)
                {
                    return;
                }

                if (resource.Status == OrderStatus.Invalid)
                {
                    throw new InvalidOperationException("Order became invalid during local issuance.");
                }

                await Task.Delay(200);
            }

            throw new TimeoutException($"Order did not become {expected} within 60 seconds.");
        }

        public static void AddTestCerts(this PfxBuilder builder)
        {
            if (root == null)
            {
                throw new InvalidOperationException("Pebble issuance root is unavailable. Await GetAcmeUriV2() successfully before exporting certificates.");
            }

            builder.AddIssuer(root);
        }

        public static void AssertExport(CertificateChain chain, IKey key)
        {
            var builder = chain.ToPfx(key);
            builder.AddTestCerts();
            using var stream = new MemoryStream(builder.Build("integration", "test-password"));
            var store = new Pkcs12StoreBuilder().Build();
            store.Load(stream, "test-password".ToCharArray());
            Assert.True(store.IsKeyEntry("integration"));
            Assert.Equal(key.ToDer(), PrivateKeyInfoFactory.CreatePrivateKeyInfo(store.GetKey("integration").Key).GetDerEncoded());
            Assert.Equal(chain.Certificate.ToDer(), store.GetCertificate("integration").Certificate.GetEncoded());
            Assert.True(store.GetCertificateChain("integration").Length >= 2);

            // Current PEM export requires a root; Pebble deliberately omits it.
            var withRoot = new CertificateChain(chain.Certificate.ToPem() +
                string.Concat(chain.Issuers.Select(i => i.ToPem())) + Encoding.UTF8.GetString(root));
            var pem = withRoot.ToPem();
            Assert.StartsWith(chain.Certificate.ToPem().Trim(), pem);
            Assert.Equal(chain.Certificate.ToDer(), new CertificateChain(pem).Certificate.ToDer());
        }
    }
}

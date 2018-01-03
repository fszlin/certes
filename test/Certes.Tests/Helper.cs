using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Certes.Acme;
using Certes.Jws;
using Certes.Pkcs;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace Certes
{
    public static class Helper
    {
        private static Lazy<HttpClient> http = new Lazy<HttpClient>(() => new HttpClient());

        internal const string PrivateKey = "MIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQCZp5PoSxdNUcB3C6rciUCX26WNcTkKeRGmvq5LD8WT2dCyF9mooV15keRIG8houKsTZmJ0wRjL6ulLU7aP6+JMblTptRJajxqh4qhfBh/Btc1ugQD4/geAYKMPbZdpcCQEd2GDJnRQFfbP6MLHWyZ3ue5yyLiHhGEL/ZwQHXH+YTpdLlWWm8b3UdtyCaXhyXQ1mtY+0ngOEV081S1DyrcBheQ6v85IVmBEqN6xFqp+dJ0k0AGqU7+MTMDHR+Al89l7VaVFY58ZFUFWilnj83kelH/2Ww0PMJnKJ0WwfDO+mQ/pHodtLwIys1aC2RNufPbsRWoiGRh2I9Ug3ZeTCPQFAgMBAAECggEANlyiCSzL/Th/uf6AQE808aUtwMl+j1R/KLnMq1TUp7cHzYJ/qNgSXKj/lX1y3Y38RLxT+A+7KKYfTN28uNWRNk5Qr3C3IiAAIacxv5DImn2qRT7R68XgPIy0FAjHaW/Z5lSgRMjNnOnwbOViSCrZBMHc+XJHSvbMaPQci10HkCIhhz993QTJm5ltH3JphnIuyBnVlUyBP5XF7B6ry1qUiApLWGRXm9+c9paUWWuUSLDTaC0t0qtJcMdkX9hA2JyFPbaQJYhedRV6XfAncai3EHV1LdmXd0vL0Y0gvrVK/tZL1yT51LKeSaMwBF/Nhv85Hc1yMbpToJY6O5rCz7g07wKBgQD1G8lUaTXlIwjPRRB0uvf0bYZF6/sawanzI6mzdOGSxsdXXpfRgatVIGJWDRFeuUSWfosVkuFfWZQXJTia6+y6RyFxVj7rCuMejWXGL6WQJDDpfxstgGGxH+q+HUU66BFY9FsS/DpffhMYdkwQgQBmBaiEnCu8fb0Zo2oZAX3IFwKBgQCge3bHe6NAav5MVIyVOPNkaHQo0El+aL1+qzC4oA3ymWk3YRTFTHLH5n15V/GwyqVoA4I86D17rDY5QY4g2pjitU1IqZMOrJm3eXuFF/8XkwYXgjAkWt/a+deL0nTl8hGGe03zx5VH6zwLYEgStn6c5Bbe/Cn989IoKXrG3VbaQwKBgQDtEj8c4dY7FjPDJi3QebayN+0TXDe3nXFftjLBXF+Bs7nDC78T6LNq1rPGP0V5tQBd/29PIo3Rx7aw3FNvpJmHYp06Hg0lEZazSlgR5KviSt70OPh0fiP/SbumvnDjlOqSe2ZLaqKbEjouAt13aQ6VnwtrmBHFcmigj6pjHUonaQKBgQCBQY/0sbdWXha+AedNFRasS5krej+HifL+QAG44mj5eeiNyyqAksdsDFAJWPT4kO9SbGkMh31ly9nMmelQuuAi0SYTHUmtqwUQCs+a7i3uneNtMdV2op7kbxDVtEelIShOaafqbljlGSk+fGjwcX5e/TMSnIVx3lzpLieOXp3iowKBgARwnBJHzCz17cU4pbE9rZdoOuybMs2piV1BKlyBerD+qSE5zAIf2J+99ytOLriDhkkrB3qg+fORgeGjYjRDd2Q/AwBZQdppaNMBaaISuiYTjP1A1v4ieTGp4gCV0kfjNouqQRcf/rjc2MsV5DLvDhxt04MBLAaoEQlr1IkmMx9v";


        private static Lazy<LogFactory> logFactory = new Lazy<LogFactory>(() =>
        {
            var config = new LoggingConfiguration();
            var memoryTarget = new MemoryTarget()
            {
                Layout = @"${message}${onexception:${exception:format=tostring}}"
            };

            config.AddTarget("logger", memoryTarget);

            var consoleRule = new LoggingRule("*", LogLevel.Debug, memoryTarget);
            config.LoggingRules.Add(consoleRule);
            return new LogFactory(config);
        });

        public static IList<string> Logs
        {
            get
            {
                var target = (MemoryTarget)logFactory.Value.Configuration.FindTargetByName("logger");
                return target.Logs;
            }
        }

        public static ILogger Logger {
            get
            {
                return logFactory.Value.GetLogger("logger");
            }
        }

        private static Uri[] StagingServers = new[]
        {
            new Uri("http://localhost:4000/directory"),
            new Uri("http://boulder-certes-ci.dymetis.com:4000/directory"),
            WellKnownServers.LetsEncryptStaging,
        };

        internal static AcmeDirectory AcmeDir = new AcmeDirectory
        {
            Meta = new AcmeDirectory.AcmeDirectoryMeta
            {
                TermsOfService = new Uri("http://example.com/tos.pdf")
            },
            NewAuthz = new Uri("http://example.com/new-authz"),
            NewCert = new Uri("http://example.com/new-cert"),
            NewReg = new Uri("http://example.com/new-reg"),
            RevokeCert = new Uri("http://example.com/revoke-cert")
        };

        private static Uri stagingServer;

        internal static async Task<Uri> GetStagingServer()
        {
            if (stagingServer != null)
            {
                return stagingServer;
            }

            var key = new AccountKey(new KeyInfo { PrivateKeyInfo = Convert.FromBase64String(PrivateKey) });
            foreach (var uri in StagingServers)
            {
                var httpSucceed = false;
                using (var http = new HttpClient())
                {
                    try
                    {
                        await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                        httpSucceed = true;
                    }
                    catch
                    {
                    }
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

                        return stagingServer = uri;
                    }
                }
            }

            throw new Exception("Staging server unavailable.");
        }

        internal static Task<AccountKey> Loadkey()
        {
            return Task.FromResult(new AccountKey(new KeyInfo { PrivateKeyInfo = Convert.FromBase64String(PrivateKey) }));
        }

        internal static void VerifyGetterSetter<TSource, TProperty>(
            this TSource source,
            Expression<Func<TSource, TProperty>> propertyLambda,
            TProperty value)
        {
            var member = propertyLambda.Body as MemberExpression;
            var propInfo = member.Member as PropertyInfo;

            propInfo.SetValue(source, value);
            var actualValue = propInfo.GetValue(source);

            Assert.Equal(value, (TProperty)actualValue);
        }
        internal static AccountKey GetAccountKey(SignatureAlgorithm algo = SignatureAlgorithm.ES256)
        {
            using (var buffer = new MemoryStream(Encoding.UTF8.GetBytes(algo.GetTestKey())))
            {
                return new AccountKey(KeyInfo.From(buffer));
            }
        }

        internal static string GetTestKey(this SignatureAlgorithm algo)
        {
            switch (algo)
            {
                case SignatureAlgorithm.ES256:
                    return Keys.ES256Key;
                case SignatureAlgorithm.ES384:
                    return Keys.ES384Key;
                case SignatureAlgorithm.ES512:
                    return Keys.ES512Key;
                default:
                    return Keys.RS256Key;
            }
        }

        internal static async Task DeployDns01(SignatureAlgorithm algo, Dictionary<string, string> tokens)
        {
            using (await http.Value.PutAsync($"http://certes-ci.dymetis.com/dns-01/{algo}", new StringContent(JsonConvert.SerializeObject(tokens)))) { }
        }
    }
}

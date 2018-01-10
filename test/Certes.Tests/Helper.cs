using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Certes.Crypto;
using Certes.Pkcs;
using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace Certes
{
    public static partial class Helper
    {
        private static readonly Lazy<HttpClient> http = new Lazy<HttpClient>(() => new HttpClient());
        private static readonly SignatureAlgorithmProvider signatureAlgorithmProvider = new SignatureAlgorithmProvider();

        // shouldn't need to add intermediate certificate
        // seems the 'up' link provided for test config is pointing to staging's cert
        internal static readonly byte[] TestCertificates = 
            File.ReadAllBytes("./Data/test-ca2.pem")
                .Concat(File.ReadAllBytes("./Data/test-root.pem")).ToArray();

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

        public static ILogger Logger
        {
            get
            {
                return logFactory.Value.GetLogger("logger");
            }
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

        internal static void AddTestCert(this PfxBuilder pfx) => pfx.AddIssuers(TestCertificates);
    }
}

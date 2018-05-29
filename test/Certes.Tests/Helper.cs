using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Certes.Crypto;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace Certes
{
    public static partial class Helper
    {
        private static readonly KeyAlgorithmProvider signatureAlgorithmProvider = new KeyAlgorithmProvider();
        private static (string Certificate, string Key)? validCertificate;

        public static IList<string> Logs
        {
            get
            {
                var target = (MemoryTarget)LogManager.Configuration.FindTargetByName("logger");
                return target.Logs;
            }
        }


        public static void SaveKey(string keyPath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(keyPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(keyPath));
            }

            File.WriteAllText(keyPath, GetTestKey(KeyAlgorithm.ES256));
        }

        public static void ConfigureLogger()
        {
            if (LogManager.Configuration == null)
            {
                var config = new LoggingConfiguration();
                var memoryTarget = new MemoryTarget()
                {
                    Layout = @"${message}${onexception:${exception:format=tostring}}"
                };

                config.AddTarget("logger", memoryTarget);

                var consoleRule = new LoggingRule("*", LogLevel.Debug, memoryTarget);
                config.LoggingRules.Add(consoleRule);
                LogManager.Configuration = config;
            }
            else
            {
                Logs.Clear();
            }
        }

        public static void VerifyGetterSetter<TSource, TProperty>(
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

        public static string GetTestKey(this KeyAlgorithm algo)
        {
            switch (algo)
            {
                case KeyAlgorithm.ES256:
                    return Keys.ES256Key;
                case KeyAlgorithm.ES384:
                    return Keys.ES384Key;
                case KeyAlgorithm.ES512:
                    return Keys.ES512Key;
                default:
                    return Keys.RS256Key;
            }
        }

        public static async Task<(string Certificate, string Key)> GetValidCert()
        {
            if (validCertificate != null)
            {
                return validCertificate.Value;
            }

            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("User-Agent", "github.com-fszlin-certes");
                var json = await http.GetStringAsync("https://api.github.com/repos/fszlin/lo0.in/releases/latest");
                var metadata = JObject.Parse(json);

                var certUrl = metadata["assets"]
                    .AsJEnumerable()
                    .Where(a => a["name"].Value<string>() == "cert.pem")
                    .Select(a => a["browser_download_url"].Value<string>())
                    .First();
                var keyUrl = metadata["assets"]
                    .AsJEnumerable()
                    .Where(a => a["name"].Value<string>() == "key.pem")
                    .Select(a => a["browser_download_url"].Value<string>())
                    .First();

                var cert = await http.GetStringAsync(certUrl);
                var key = await http.GetStringAsync(keyUrl);

                validCertificate = (cert, key);
                return validCertificate.Value;
            }
        }
    }
}
